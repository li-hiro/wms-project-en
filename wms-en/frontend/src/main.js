import Vue from 'vue'
import ElementUI from 'element-ui'
import enLocale from 'element-ui/lib/locale/lang/en'
import 'element-ui/lib/theme-chalk/index.css'
import App from './App.vue'
import router from './router'
import store from './store'
import './assets/styles.css'

Vue.use(ElementUI, { size: 'small', locale: enLocale })

/**
 * 按钮级权限指令：v-permission="'material:edit'"。
 * 无权限时移除元素，避免误操作入口暴露。
 */
Vue.directive('permission', {
  inserted(el, binding) {
    const code = binding.value
    if (!code) return
    const permissions = store.getters.permissions || []
    if (!permissions.includes('*') && !permissions.includes(code)) {
      el.parentNode && el.parentNode.removeChild(el)
    }
  }
})

Vue.config.productionTip = false

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app')
