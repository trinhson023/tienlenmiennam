<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import axios from 'axios'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const username = ref('')
const password = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    await auth.login(username.value, password.value)
    await router.push((route.query.redirect as string) || '/')
  } catch (e) {
    error.value = axios.isAxiosError(e) ? (e.response?.data?.message || 'Đăng nhập thất bại.') : 'Đăng nhập thất bại.'
  }
}
</script>

<template>
  <main class="auth-page">
    <section class="auth-card">
      <span class="eyebrow">ROYAL GAME V3</span>
      <h1>Đăng nhập</h1>
      <p class="muted">IdentityService · JWT access + refresh token</p>
      <form @submit.prevent="submit" class="auth-form">
        <label>Username<input v-model.trim="username" autocomplete="username" placeholder="son023" /></label>
        <label>Mật khẩu<input v-model="password" type="password" autocomplete="current-password" placeholder="Tối thiểu 8 ký tự" /></label>
        <p v-if="error" class="error">{{ error }}</p>
        <button class="primary" :disabled="auth.busy">{{ auth.busy ? 'Đang đăng nhập…' : 'Đăng nhập' }}</button>
      </form>
      <p class="auth-switch">Chưa có tài khoản? <RouterLink to="/register">Tạo tài khoản</RouterLink></p>
    </section>
  </main>
</template>
