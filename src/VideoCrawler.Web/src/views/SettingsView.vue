<template>
  <div class="settings-view">
    <el-card>
      <template #header>
        <span>⚙️ 系统设置</span>
      </template>

      <el-form label-width="150px">
        <el-form-item label="数据库连接">
          <el-input v-model="settings.dbConnection" />
        </el-form-item>
        <el-form-item label="缓存路径">
          <el-input v-model="settings.cachePath" />
        </el-form-item>
        <el-form-item label="最大缓存大小">
          <el-input v-model="settings.maxCacheSize" placeholder="10GB" />
        </el-form-item>
        <el-form-item label="缓存过期时间">
          <el-input v-model="settings.cacheExpiration" placeholder="30 天" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="saveSettings">保存设置</el-button>
        </el-form-item>
      </el-form>

      <el-divider />

      <el-form label-width="150px">
        <el-form-item label="系统信息">
          <div class="system-info">
            <p>📦 版本：v1.0.0</p>
            <p>🔧 架构：.NET 10 + Vue 3 DDD</p>
            <p>📊 视频总数：{{ stats.totalVideos }}</p>
            <p>💾 已缓存：{{ stats.cachedVideos }}</p>
            <p>📈 缓存大小：{{ formatSize(stats.cacheSize) }}</p>
          </div>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'

const settings = ref({
  dbConnection: 'Data Source=vodcrawler.db',
  cachePath: './cache',
  maxCacheSize: '10GB',
  cacheExpiration: '30 天'
})

const stats = ref({
  totalVideos: 0,
  cachedVideos: 0,
  cacheSize: 0
})

const saveSettings = () => {
  alert('设置保存功能开发中...')
}

const formatSize = (bytes: number) => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
}

onMounted(() => {
  // TODO: 加载实际统计信息
})
</script>

<style scoped>
.settings-view {
  max-width: 800px;
  margin: 0 auto;
}

.system-info {
  background: #f5f7fa;
  padding: 20px;
  border-radius: 4px;
}

.system-info p {
  margin: 10px 0;
}
</style>
