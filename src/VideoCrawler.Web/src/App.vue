<template>
  <div id="app">
    <el-container>
      <el-header>
        <div class="header-content">
          <h1>🎬 VideoCrawler</h1>
          <el-menu mode="horizontal" :router="true" :default-active="$route.path">
            <el-menu-item index="/">📺 视频列表</el-menu-item>
            <el-menu-item index="/tasks">📋 爬取任务</el-menu-item>
            <el-menu-item index="/workers">🤖 工作节点</el-menu-item>
            <el-menu-item index="/settings">⚙️ 设置</el-menu-item>
          </el-menu>
        </div>
      </el-header>
      
      <el-main>
        <router-view />
      </el-main>
      
      <el-footer>
        <p>VideoCrawler v1.0 - .NET 10 + Vue 3 DDD 架构</p>
      </el-footer>
    </el-container>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
#app {
  min-height: 100vh;
}

.el-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 0 20px;
}

.header-content {
  display: flex;
  align-items: center;
  height: 100%;
  gap: 40px;
}

.header-content h1 {
  margin: 0;
  font-size: 24px;
}

.el-menu {
  background: transparent;
  border: none;
  flex: 1;
}

.el-menu-item {
  color: white;
}

.el-menu-item:hover {
  background: rgba(255, 255, 255, 0.2);
}

.el-main {
  padding: 20px;
  background: #f5f7fa;
  min-height: calc(100vh - 120px);
}

.el-footer {
  background: #333;
  color: #999;
  text-align: center;
  padding: 10px;
}
</style>
