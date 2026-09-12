<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import axios from 'axios'

const auth = useAuthStore()
const router = useRouter()
const username = ref('')
const displayName = ref('')
const password = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await auth.register(username.value, displayName.value, password.value)
    await router.push('/')
  } catch (e) {
    error.value = axios.isAxiosError(e) ? (e.response?.data?.message || 'Tạo tài khoản thất bại.') : 'Tạo tài khoản thất bại.'
  }
}
</script>

<template>
  <main class="auth-page">
    <section class="auth-card">
      <span class="eyebrow">M2 · IDENTITY</span>
      <h1>Tạo tài khoản</h1>
      <p class="muted">Tài khoản này sẽ dùng chung cho Tiến Lên, Sâm, Cờ Tướng và các game sau.</p>
      <form @submit.prevent="submit" class="auth-form">
        <label>Username<input v-model.trim="username" autocomplete="username" placeholder="3-32 ký tự" /></label>
        <label>Tên hiển thị<input v-model.trim="displayName" autocomplete="name" placeholder="Sơn" /></label>
        <label>Mật khẩu<input v-model="password" type="password" autocomplete="new-password" placeholder="Tối thiểu 8 ký tự" /></label>
        <p v-if="error" class="error">{{ error }}</p>
        <button class="primary" :disabled="auth.busy">{{ auth.busy ? 'Đang tạo…' : 'Tạo tài khoản' }}</button>
      </form>
      <p class="auth-switch">Đã có tài khoản? <RouterLink to="/login">Đăng nhập</RouterLink></p>
    </section>
  </main>
</template>
