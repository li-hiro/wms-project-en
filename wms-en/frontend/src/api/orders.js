import request from './request'

/* ---------------- 入库 ---------------- */

export const inboundApi = {
  page: params => request({ url: '/inbound-orders', method: 'get', params }),
  get: id => request({ url: `/inbound-orders/${id}`, method: 'get' }),
  create: data => request({ url: '/inbound-orders', method: 'post', data }),
  update: (id, data) => request({ url: `/inbound-orders/${id}`, method: 'put', data }),
  confirm: id => request({ url: `/inbound-orders/${id}/confirm`, method: 'post' }),
  complete: id => request({ url: `/inbound-orders/${id}/complete`, method: 'post' }),
  cancel: id => request({ url: `/inbound-orders/${id}/cancel`, method: 'post' }),
  remove: id => request({ url: `/inbound-orders/${id}`, method: 'delete' })
}

/* ---------------- 出库 ---------------- */

export const outboundApi = {
  page: params => request({ url: '/outbound-orders', method: 'get', params }),
  get: id => request({ url: `/outbound-orders/${id}`, method: 'get' }),
  create: data => request({ url: '/outbound-orders', method: 'post', data }),
  update: (id, data) => request({ url: `/outbound-orders/${id}`, method: 'put', data }),
  confirm: id => request({ url: `/outbound-orders/${id}/confirm`, method: 'post' }),
  complete: id => request({ url: `/outbound-orders/${id}/complete`, method: 'post' }),
  cancel: id => request({ url: `/outbound-orders/${id}/cancel`, method: 'post' }),
  remove: id => request({ url: `/outbound-orders/${id}`, method: 'delete' })
}

/* ---------------- 盘点 ---------------- */

export const stockTakeApi = {
  page: params => request({ url: '/stocktake-orders', method: 'get', params }),
  get: id => request({ url: `/stocktake-orders/${id}`, method: 'get' }),
  create: data => request({ url: '/stocktake-orders', method: 'post', data }),
  saveItems: (id, data) => request({ url: `/stocktake-orders/${id}/items`, method: 'put', data }),
  complete: id => request({ url: `/stocktake-orders/${id}/complete`, method: 'post' }),
  cancel: id => request({ url: `/stocktake-orders/${id}/cancel`, method: 'post' }),
  remove: id => request({ url: `/stocktake-orders/${id}`, method: 'delete' })
}

/* ---------------- 调拨 ---------------- */

export const transferApi = {
  page: params => request({ url: '/transfer-orders', method: 'get', params }),
  get: id => request({ url: `/transfer-orders/${id}`, method: 'get' }),
  create: data => request({ url: '/transfer-orders', method: 'post', data }),
  update: (id, data) => request({ url: `/transfer-orders/${id}`, method: 'put', data }),
  complete: id => request({ url: `/transfer-orders/${id}/complete`, method: 'post' }),
  cancel: id => request({ url: `/transfer-orders/${id}/cancel`, method: 'post' }),
  remove: id => request({ url: `/transfer-orders/${id}`, method: 'delete' })
}
