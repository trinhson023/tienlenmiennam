const ranks = ['3','4','5','6','7','8','9','T','J','Q','K','A','2']

type Combination = 'single' | 'pair' | 'triple' | 'straight' | 'four' | 'three-pair' | 'four-pair'

function rankOf(card: string) { return card.slice(0, -1).toUpperCase() }

function isConsecutive(values: string[]) {
  if (!values.length) return false
  const first = ranks.indexOf(values[0])
  if (first < 0) return false
  return ranks.slice(first, first + values.length).join('|') === values.join('|')
}

function detectCombination(cards: string[]): Combination | null {
  if (!cards.length) return null
  const cardRanks = cards.map(rankOf)
  if (cards.length === 1) return 'single'
  if (cards.length === 2 && cardRanks.every(rank => rank === cardRanks[0])) return 'pair'
  if (cards.length === 3 && cardRanks.every(rank => rank === cardRanks[0])) return 'triple'

  if (cards.length >= 3 && cardRanks.every(rank => rank !== '2')) {
    const ordered = [...cardRanks].sort((a, b) => ranks.indexOf(a) - ranks.indexOf(b))
    if (isConsecutive(ordered)) return 'straight'
  } else if (cardRanks.some(rank => rank === '2')) {
    // Preserve the legacy rule quirk: combinations containing rank 2 are not bombs/runs.
    return null
  }

  if (cards.length === 4 && cardRanks.every(rank => rank === cardRanks[0])) return 'four'

  const grouped = new Map<string, number>()
  for (const rank of cardRanks) grouped.set(rank, (grouped.get(rank) || 0) + 1)
  const onlyPairs = [...grouped.values()].every(count => count === 2)
  const distinctRanks = [...grouped.keys()].sort((a, b) => ranks.indexOf(a) - ranks.indexOf(b))
  if (onlyPairs && isConsecutive(distinctRanks)) {
    if (cards.length === 6) return 'three-pair'
    if (cards.length === 8) return 'four-pair'
  }
  return null
}

export function isLegacyChop(previousCenter: string[], nextCenter: string[]) {
  if (!previousCenter.length || !nextCenter.length) return false
  const nextCombo = detectCombination(nextCenter)
  const previousCombo = detectCombination(previousCenter)
  const previousRanks = previousCenter.map(rankOf)
  const previousAllTwos = previousRanks.every(rank => rank === '2')

  if (previousCenter.length === 1 && previousRanks[0] === '2') {
    return nextCombo === 'three-pair' || nextCombo === 'four' || nextCombo === 'four-pair'
  }
  if (previousCenter.length === 2 && previousAllTwos) {
    return nextCombo === 'four' || nextCombo === 'four-pair'
  }
  if (previousCombo === 'three-pair') {
    return nextCombo === 'four' || nextCombo === 'four-pair'
  }
  if (previousCombo === 'four') return nextCombo === 'four-pair'
  return false
}
