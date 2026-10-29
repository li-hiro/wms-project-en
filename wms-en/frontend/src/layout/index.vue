<template>
  <div class="layout">
    <aside class="layout-sidebar" :class="{ collapsed: !sidebarOpened }">
      <div class="brand">
        <span v-if="sidebarOpened">WMS Warehouse Management</span>
        <span v-else>WMS</span>
      </div>
      <el-menu
        :default-active="activeMenu"
        :collapse="!sidebarOpened"
        :collapse-transition="false"
        router
        unique-opened
        background-color="#1f2d3d"
        text-color="#c0c4cc"
        active-text-color="#409eff"
      >
        <template v-for="item in menus">
          <el-submenu v-if="item.children && item.children.length" :key="item.id" :index="String(item.id)">
            <template slot="title">
              <i :class="item.icon"></i>
              <span slot="title">{{ item.name }}</span>
            </template>
            <el-menu-item v-for="child in item.children" :key="child.id" :index="child.path">
              <i :class="child.icon"></i>
              <span slot="title">{{ child.name }}</span>
            </el-menu-item>
          </el-submenu>
          <el-menu-item v-else :key="item.id" :index="item.path">
            <i :class="item.icon"></i>
            <span slot="title">{{ item.name }}</span>
          </el-menu-item>
        </template>
      </el-menu>
    </aside>

    <section class="layout-main">
      <header class="layout-header">
        <div class="header-left">
          <i
            class="el-icon-s-fold toggle"
            :class="{ 'el-icon-s-unfold': !sidebarOpened }"
            @click="toggleSidebar"
          ></i>
          <span class="page-title">{{ currentTitle }}</span>
        </div>
        <div class="header-right">
          <span class="welcome">{{ profile.nickName || profile.userName }}</span>
          <el-dropdown trigger="click" @command="handleCommand">
            <el-avatar :size="30" class="avatar">{{ avatarText }}</el-avatar>
            <el-dropdown-menu slot="dropdown">
              <el-dropdown-item command="password">Change Password</el-dropdown-item>
              <el-dropdown-item command="logout" divided>Sign Out</el-dropdown-item>
            </el-dropdown-menu>
          </el-dropdown>
        </div>
      </header>

      <main class="layout-content">
        <router-view />
      </main>
    </section>

    <el-dialog title="Change Password" :visible.sync="passwordVisible" width="420px">
      <el-form ref="passwordForm" :model="passwordForm" :rules="passwordRules" label-width="120px">
        <el-form-item label="Old Password" prop="oldPassword">
          <el-input v-model="passwordForm.oldPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="New Password" prop="newPassword">
          <el-input v-model="passwordForm.newPassword" type="password" show-password />
        </el-form-item>
      </el-form>
      <span slot="footer">
        <el-button @click="passwordVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="passwordLoading" @click="submitPassword">OK</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { changePassword } from '@/api/auth'

export default {
  name: 'Layout',
  data() {
    return {
      passwordVisible: false,
      passwordLoading: false,
      passwordForm: { oldPassword: '', newPassword: '' },
      passwordRules: {
        oldPassword: [{ required: true, message: 'Please enter your old password', trigger: 'blur' }],
        newPassword: [
          { required: true, message: 'Please enter a new password', trigger: 'blur' },
          { min: 6, message: 'New password must be at least 6 characters', trigger: 'blur' }
        ]
      }
    }
  },
  computed: {
    menus() {
      return this.$store.getters.menus || []
    },
    profile() {
      return this.$store.getters.profile || {}
    },
    sidebarOpened() {
      return this.$store.getters.sidebarOpened
    },
    activeMenu() {
      return this.$route.path
    },
    currentTitle() {
      return (this.$route.meta && this.$route.meta.title) || 'Dashboard'
    },
    avatarText() {
      const name = this.profile.nickName || this.profile.userName || 'U'
      return name.substring(0, 1).toUpperCase()
    }
  },
  methods: {
    toggleSidebar() {
      this.$store.commit('app/TOGGLE_SIDEBAR')
    },
    handleCommand(command) {
      if (command === 'logout') {
        this.logout()
      } else if (command === 'password') {
        this.passwordForm = { oldPassword: '', newPassword: '' }
        this.passwordVisible = true
      }
    },
    logout() {
      this.$confirm('Sign out of the current account?', 'Confirm', { type: 'warning' })
        .then(() => {
          this.$store.dispatch('user/logout')
          this.$router.replace('/login')
        })
        .catch(() => {})
    },
    submitPassword() {
      this.$refs.passwordForm.validate(async valid => {
        if (!valid) return
        this.passwordLoading = true
        try {
          await changePassword(this.passwordForm)
          this.$message.success('Password changed. Please sign in again.')
          this.passwordVisible = false
          this.$store.dispatch('user/logout')
          this.$router.replace('/login')
        } finally {
          this.passwordLoading = false
        }
      })
    }
  }
}
</script>

<style scoped>
.layout {
  display: flex;
  height: 100%;
}

.layout-sidebar {
  width: 220px;
  background: #1f2d3d;
  transition: width 0.2s;
  overflow-x: hidden;
}

.layout-sidebar.collapsed {
  width: 64px;
}

.brand {
  height: 56px;
  line-height: 56px;
  text-align: center;
  color: #fff;
  font-size: 16px;
  font-weight: 600;
  letter-spacing: 1px;
}

.layout-sidebar .el-menu {
  border-right: none;
}

.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.layout-header {
  height: 56px;
  background: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  box-shadow: 0 1px 4px rgba(0, 21, 41, 0.08);
}

.header-left {
  display: flex;
  align-items: center;
}

.toggle {
  font-size: 20px;
  cursor: pointer;
  margin-right: 16px;
  color: #606266;
}

.page-title {
  font-size: 15px;
  font-weight: 600;
}

.header-right {
  display: flex;
  align-items: center;
}

.welcome {
  color: #606266;
  margin-right: 12px;
}

.avatar {
  background: #409eff;
  cursor: pointer;
  font-size: 13px;
}

.layout-content {
  flex: 1;
  overflow: auto;
}
</style>
