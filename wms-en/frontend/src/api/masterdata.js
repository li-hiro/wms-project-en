import request from './request'

/* ---------------- 基础资料 ---------------- */

export const categoryApi = {
  list: () => request({ url: '/material-categories', method: 'get' }),
  create: data => request({ url: '/material-categories', method: 'post', data }),
  update: (id, data) => request({ url: `/material-categories/${id}`, method: 'put', data }),
  remove: id => request({ url: `/material-categories/${id}`, method: 'delete' })
}

export const materialApi = {
  page: params => request({ url: '/materials', method: 'get', params }),
  options: params => request({ url: '/materials/options', method: 'get', params }),
  create: data => request({ url: '/materials', method: 'post', data }),
  update: (id, data) => request({ url: `/materials/${id}`, method: 'put', data }),
  remove: id => request({ url: `/materials/${id}`, method: 'delete' })
}

export const supplierApi = {
  page: params => request({ url: '/suppliers', method: 'get', params }),
  options: () => request({ url: '/suppliers/options', method: 'get' }),
  create: data => request({ url: '/suppliers', method: 'post', data }),
  update: (id, data) => request({ url: `/suppliers/${id}`, method: 'put', data }),
  remove: id => request({ url: `/suppliers/${id}`, method: 'delete' })
}

export const customerApi = {
  page: params => request({ url: '/customers', method: 'get', params }),
  options: () => request({ url: '/customers/options', method: 'get' }),
  create: data => request({ url: '/customers', method: 'post', data }),
  update: (id, data) => request({ url: `/customers/${id}`, method: 'put', data }),
  remove: id => request({ url: `/customers/${id}`, method: 'delete' })
}

/* ---------------- 仓库配置 ---------------- */

export const warehouseApi = {
  page: params => request({ url: '/warehouses', method: 'get', params }),
  options: () => request({ url: '/warehouses/options', method: 'get' }),
  create: data => request({ url: '/warehouses', method: 'post', data }),
  update: (id, data) => request({ url: `/warehouses/${id}`, method: 'put', data }),
  remove: id => request({ url: `/warehouses/${id}`, method: 'delete' })
}

export const locationApi = {
  page: params => request({ url: '/locations', method: 'get', params }),
  options: params => request({ url: '/locations/options', method: 'get', params }),
  create: data => request({ url: '/locations', method: 'post', data }),
  update: (id, data) => request({ url: `/locations/${id}`, method: 'put', data }),
  remove: id => request({ url: `/locations/${id}`, method: 'delete' })
}
