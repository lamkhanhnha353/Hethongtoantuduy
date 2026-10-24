<template>
  <div class="flex h-screen bg-orange-50 overflow-hidden">
    <!-- Mobile overlay -->
    <transition name="fade">
      <div
        v-if="isMobileOpen"
        class="fixed inset-0 bg-black/40 z-20 lg:hidden"
        @click="closeMobileSidebar"
      />
    </transition>

    <!-- Sidebar -->
    <Sidebar
      :collapsed="isSidebarCollapsed"
      :mobile-open="isMobileOpen"
      @close-mobile="closeMobileSidebar"
    />

    <!-- Main wrapper -->
    <div class="flex flex-col flex-1 min-w-0 overflow-hidden">
      <!-- Header -->
      <Header
        :sidebar-collapsed="isSidebarCollapsed"
        @toggle-sidebar="toggleSidebar"
        @toggle-mobile-sidebar="toggleMobileSidebar"
      />

      <!-- Content area -->
      <main class="flex-1 overflow-y-auto p-4 md:p-6">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import Sidebar from '@/components/Sidebar.vue'
import Header from '@/components/Header.vue'

// ─── State ───────────────────────────────────────────────────────────────────
const isSidebarCollapsed = ref(false)
const isMobileOpen = ref(false)

// ─── Helpers ─────────────────────────────────────────────────────────────────
function toggleSidebar() {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
}

function toggleMobileSidebar() {
  isMobileOpen.value = !isMobileOpen.value
}

function closeMobileSidebar() {
  isMobileOpen.value = false
}

// Close mobile sidebar when resizing to desktop
function handleResize() {
  if (window.innerWidth >= 1024) {
    isMobileOpen.value = false
  }
}

onMounted(() => window.addEventListener('resize', handleResize))
onUnmounted(() => window.removeEventListener('resize', handleResize))
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
