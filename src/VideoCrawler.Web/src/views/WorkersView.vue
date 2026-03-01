<template>
  <div class="workers-view">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>🤖 工作节点</span>
          <el-button type="primary" @click="registerWorker">➕ 注册节点</el-button>
        </div>
      </template>

      <el-table :data="workers" stripe>
        <el-table-column prop="workerId" label="节点 ID" />
        <el-table-column prop="workerName" label="节点名称" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 'Idle' ? 'success' : row.status === 'Busy' ? 'warning' : 'danger'">
              {{ row.status === 'Idle' ? '空闲' : row.status === 'Busy' ? '忙碌' : '离线' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="currentTaskId" label="当前任务" />
        <el-table-column prop="completedTasks" label="完成任务" width="100" />
        <el-table-column prop="failedTasks" label="失败任务" width="100" />
        <el-table-column prop="lastHeartbeat" label="最后心跳" width="180" />
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'

const workers = ref<any[]>([])

const loadWorkers = async () => {
  try {
    const response = await axios.get('/api/crawlerTasks/workers')
    workers.value = response.data || []
  } catch (error) {
    console.error('加载节点失败:', error)
  }
}

const registerWorker = async () => {
  const name = prompt('请输入节点名称:')
  if (!name) return

  // TODO: 实现注册接口
  alert('节点注册功能开发中...')
}

onMounted(() => {
  loadWorkers()
})
</script>

<style scoped>
.workers-view {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
