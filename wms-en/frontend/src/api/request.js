import axios from 'axios'
import store from '@/store'
import { Message } from 'element-ui'

/**
 * 统一 HTTP 客户端。
 * 约定后端响应结构：{ success, code, message, data, traceId }。
 * - 成功：直接把 data 返回给调用方
 * - 401：清除登录态并跳转登录页
 * - 其它失败：抛出错误并弹出提示
 */
const service = axios.create({
  baseURL: '/api',
  timeout: 20000
})

service.interceptors.request.use(
  config => {
    const token = store.getters.token
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => Promise.reject(error)
)

service.interceptors.response.use(
  response => {
    const payload = response.data
    if (payload && typeof payload.success === 'boolean') {
      if (payload.success) {
        return payload.data
      }
      Message.error(payload.message || 'Request failed')
      return Promise.reject(new Error(payload.message || 'Request failed'))
    }
    return payload
  },
  error => {
    const status = error.response && error.response.status
    const payload = error.response && error.response.data

    if (status === 401) {
      store.dispatch('user/resetToken')
      Message.error((payload && payload.message) || 'Your session has expired. Please sign in again.')
      if (location.pathname !== '/login') {
        location.href = '/login'
      }
      return Promise.reject(error)
    }

    const message = (payload && payload.message) || error.message || 'Network error. Please try again later.'
    Message.error(status === 403 ? 'You do not have permission to access this resource.' : message)
    return Promise.reject(error)
  }
)

export default service
