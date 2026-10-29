/** 应用级 UI 状态 */
const state = {
  sidebarOpened: true
}

const mutations = {
  TOGGLE_SIDEBAR(state) {
    state.sidebarOpened = !state.sidebarOpened
  }
}

export default {
  namespaced: true,
  state,
  mutations
}
