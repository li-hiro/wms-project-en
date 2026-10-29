<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">Stock is aggregated by material and warehouse. Items below safety stock appear in this alert list.</span>
        <el-button icon="el-icon-refresh" @click="loadData">Refresh</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="materialCode" label="Material Code" width="140" />
        <el-table-column prop="materialName" label="Material Name" min-width="170" show-overflow-tooltip />
        <el-table-column prop="spec" label="Spec" min-width="140" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="Warehouse" width="130" />
        <el-table-column prop="onHandQty" label="On-hand Qty" width="110" align="right" />
        <el-table-column prop="lockedQty" label="Locked Qty" width="110" align="right" />
        <el-table-column prop="safetyStock" label="Safety Stock" width="110" align="right" />
        <el-table-column prop="shortageQty" label="Shortage Qty" width="110" align="right">
          <template slot-scope="{ row }">
            <span class="text-danger">{{ row.shortageQty }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="unit" label="Unit" width="70" align="center" />
      </el-table>

      <div class="empty-tip" v-if="!loading && !list.length">No materials are currently below safety stock</div>
    </div>
  </div>
</template>

<script>
import { inventoryApi } from '@/api/inventory'

export default {
  name: 'InventoryAlerts',
  data() {
    return {
      loading: false,
      list: []
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        this.list = await inventoryApi.alerts()
      } finally {
        this.loading = false
      }
    }
  }
}
</script>

<style scoped>
.empty-tip {
  padding: 24px 0;
  text-align: center;
  color: #909399;
}
</style>
