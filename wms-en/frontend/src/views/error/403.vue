<template>
  <div class="error-page">
    <h1>403</h1>
    <p>Access Denied: your account does not have permission to view this page</p>
    <el-button type="primary" @click="$router.replace('/dashboard')">Back to Home</el-button>
  </div>
</template>

<script>
export default {
  name: 'Forbidden'
}
</script>

<style scoped>
.error-page {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #606266;
}
.error-page h1 {
  font-size: 64px;
  margin: 0 0 8px;
  color: #409eff;
}
.error-page p {
  margin: 0 0 24px;
}
</style>
