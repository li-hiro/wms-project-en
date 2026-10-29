import request from './request'

/* ---------------- 用户 ---------------- */

export const userApi = {
  page: params => request({ url: '/users', method: 'get', params }),
  create: data => request({ url: '/users', method: 'post', data }),
  update: (id, data) => request({ url: `/users/${id}`, method: 'put', data }),
  remove: id => request({ url: `/users/${id}`, method: 'delete' }),
  resetPassword: (id, data) => request({ url: `/users/${id}/reset-password`, method: 'post', data }),
  toggleStatus: (id, status) => request({ url: `/users/${id}/status`, method: 'put', params: { status } })
}

/* ---------------- 角色与权限 ---------------- */

export const roleApi = {
  list: () => request({ url: '/roles', method: 'get' }),
  permissions: () => request({ url: '/roles/permissions', method: 'get' }),
  create: data => request({ url: '/roles', method: 'post', data }),
  update: (id, data) => request({ url: `/roles/${id}`, method: 'put', data }),
  remove: id => request({ url: `/roles/${id}`, method: 'delete' })
}

/* ---------------- 菜单 ---------------- */

export const menuApi = {
  tree: () => request({ url: '/menus', method: 'get' }),
  create: data => request({ url: '/menus', method: 'post', data }),
  update: (id, data) => request({ url: `/menus/${id}`, method: 'put', data }),
  remove: id => request({ url: `/menus/${id}`, method: 'delete' })
}

/* ---------------- 操作日志 ---------------- */

export const logApi = {
  page: params => request({ url: '/logs', method: 'get', params }),
  clear: keepDays => request({ url: '/logs', method: 'delete', params: { keepDays } })
}

/* ---------------- 插件 ---------------- */

export const pluginApi = {
  list: () => request({ url: '/plugins', method: 'get' }),
  update: (id, data) => request({ url: `/plugins/${id}`, method: 'put', data }),
  execute: id => request({ url: `/plugins/${id}/execute`, method: 'post' })
}
