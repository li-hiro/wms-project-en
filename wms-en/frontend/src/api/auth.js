import request from './request'

/** 登录 */
export function login(data) {
  return request({ url: '/auth/login', method: 'post', data })
}

/** 当前用户信息 */
export function getProfile() {
  return request({ url: '/auth/profile', method: 'get' })
}

/** 当前用户菜单树 */
export function getMenus() {
  return request({ url: '/auth/menus', method: 'get' })
}

/** 修改密码 */
export function changePassword(data) {
  return request({ url: '/auth/change-password', method: 'post', data })
}
