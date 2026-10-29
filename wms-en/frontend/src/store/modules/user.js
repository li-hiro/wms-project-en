import { login, getProfile, getMenus } from '@/api/auth'

const TOKEN_KEY = 'wms_token'

/**
 * 用户状态：令牌、个人资料、权限与菜单。
 * 所有权限判断统一走 hasPermission，管理员拥有通配权限 *。
 */
const state = {
  token: localStorage.getItem(TOKEN_KEY) || '',
  profile: {},
  permissions: [],
  menus: []
}

const mutations = {
  SET_TOKEN(state, token) {
    state.token = token || ''
    if (token) {
      localStorage.setItem(TOKEN_KEY, token)
    } else {
      localStorage.removeItem(TOKEN_KEY)
    }
  },
  SET_PROFILE(state, profile) {
    state.profile = profile || {}
    state.permissions = (profile && profile.permissions) || []
  },
  SET_MENUS(state, menus) {
    state.menus = menus || []
  }
}

const actions = {
  /** 登录并同步资料与菜单 */
  async login({ commit, dispatch }, form) {
    const result = await login(form)
    commit('SET_TOKEN', result.token)
    commit('SET_PROFILE', result.user)
    await dispatch('loadMenus')
    return result
  },

  /** 拉取当前用户资料 */
  async loadProfile({ commit }) {
    const profile = await getProfile()
    commit('SET_PROFILE', profile)
    return profile
  },

  /** 拉取当前用户菜单 */
  async loadMenus({ commit }) {
    const menus = await getMenus()
    commit('SET_MENUS', menus)
    return menus
  },

  /** 退出登录，清理本地状态 */
  logout({ commit }) {
    commit('SET_TOKEN', '')
    commit('SET_PROFILE', {})
    commit('SET_MENUS', [])
  },

  /** 令牌失效时清理登录态 */
  resetToken({ commit }) {
    commit('SET_TOKEN', '')
    commit('SET_PROFILE', {})
    commit('SET_MENUS', [])
  }
}

export default {
  namespaced: true,
  state,
  mutations,
  actions
}
