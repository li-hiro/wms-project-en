const path = require('path')

/**
 * 前端工程配置：
 * - 开发服务器固定 8080 端口
 * - 统一把 /api 代理到本地后端 5080，避免跨域
 */
module.exports = {
  publicPath: '/',
  outputDir: 'dist',
  lintOnSave: false,
  productionSourceMap: false,
  devServer: {
    port: 8080,
    open: false,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true
      }
    }
  },
  configureWebpack: {
    resolve: {
      alias: {
        '@': path.resolve(__dirname, 'src')
      }
    }
  }
}
