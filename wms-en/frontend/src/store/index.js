import Vue from 'vue'
import Vuex from 'vuex'
import user from './modules/user'
import app from './modules/app'

Vue.use(Vuex)

export default new Vuex.Store({
  modules: {
    user,
    app
  },
  // 根级 getter 便于在非命名空间场景直接访问登录态
  getters: {
    token: state => state.user.token,
    profile: state => state.user.profile,
    permissions: state => state.user.permissions,
    menus: state => state.user.menus,
    sidebarOpened: state => state.app.sidebarOpened
  }
})
