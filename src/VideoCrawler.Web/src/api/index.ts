import axios from 'axios'
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios'

const service: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
  timeout: 30000
})

// 请求拦截器
service.interceptors.request.use(
  config => {
    // 在发送请求之前做些什么
    return config
  },
  error => {
    console.error('请求错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  response => {
    return response.data
  },
  error => {
    console.error('响应错误:', error.message)
    return Promise.reject(error)
  }
)

export default service

// 视频相关 API
export const videoApi = {
  getList(params: { page: number; pageSize: number }) {
    return service.get('/videos', { params })
  },
  
  getById(id: string) {
    return service.get(`/videos/${id}`)
  },
  
  getByCategory(category: string, params: { page: number; pageSize: number }) {
    return service.get(`/videos/category/${category}`, { params })
  },
  
  search(keyword: string, params: { page: number; pageSize: number }) {
    return service.get('/videos/search', { params: { keyword, ...params } })
  },
  
  getCached(params: { page: number; pageSize: number }) {
    return service.get('/videos/cached', { params })
  }
}

// 任务相关 API
export const taskApi = {
  create(data: { targetUrl: string; taskType: string }) {
    return service.post('/crawlerTasks', data)
  },
  
  getList(params?: { status?: string; limit?: number }) {
    return service.get('/crawlerTasks', { params })
  },
  
  getById(id: string) {
    return service.get(`/crawlerTasks/${id}`)
  },
  
  start(id: string) {
    return service.post(`/crawlerTasks/${id}/start`)
  },
  
  cancel(id: string) {
    return service.post(`/crawlerTasks/${id}/cancel`)
  },
  
  retry(id: string) {
    return service.post(`/crawlerTasks/${id}/retry`)
  }
}

// 调试相关 API
export const debugApi = {
  analyze(url: string) {
    return service.get('/debug/analyze', { params: { url } })
  },
  
  testList(url: string, maxCount?: number) {
    return service.get('/debug/test-list', { params: { url, maxCount } })
  },
  
  testDetail(url: string) {
    return service.get('/debug/test-detail', { params: { url } })
  }
}
