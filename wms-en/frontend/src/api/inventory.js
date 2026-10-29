import request from './request'

/** 库存台账 */
export const inventoryApi = {
  page: params => request({ url: '/inventory', method: 'get', params }),
  transactions: params => request({ url: '/inventory/transactions', method: 'get', params }),
  summary: () => request({ url: '/inventory/summary', method: 'get' }),
  alerts: () => request({ url: '/inventory/alerts', method: 'get' }),
  recent: take => request({ url: '/inventory/recent', method: 'get', params: { take } })
}

/** 报表 */
export const reportApi = {
  dashboard: () => request({ url: '/reports/dashboard', method: 'get' }),
  stockSummary: params => request({ url: '/reports/stock-summary', method: 'get', params }),
  inOutSummary: params => request({ url: '/reports/in-out-summary', method: 'get', params }),
  trend: days => request({ url: '/reports/trend', method: 'get', params: { days } }),
  topMaterials: params => request({ url: '/reports/top-materials', method: 'get', params })
}
