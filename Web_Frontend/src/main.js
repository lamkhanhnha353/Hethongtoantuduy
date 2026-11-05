import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './assets/main.css'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import GoogleSignInPlugin from 'vue3-google-signin'

const app = createApp(App)

app.use(router)
app.use(ElementPlus)
app.use(GoogleSignInPlugin, {
  clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID,
})

app.mount('#app')
