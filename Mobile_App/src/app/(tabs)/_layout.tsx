import { Feather, Ionicons } from '@expo/vector-icons';
import { Tabs } from 'expo-router';
import { useNotificationUnreadCountQuery } from '@/hooks/useNotifications';

export default function TabsLayout() {
  const { data: unreadCountData } = useNotificationUnreadCountQuery();
  const unreadCount = unreadCountData?.unreadCount ?? 0;

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: '#0284c7',
        tabBarInactiveTintColor: '#94a3b8',
        tabBarStyle: { borderTopColor: '#e2e8f0', paddingTop: 4, paddingBottom: 2 },
        tabBarIconStyle: {
          width: 28,
          height: 28,
          alignItems: 'center',
          justifyContent: 'center',
        },
        tabBarLabelStyle: { fontSize: 11, fontWeight: '500', marginBottom: 4 },
      }}
    >
      <Tabs.Screen
        name="home"
        options={{
          title: 'Trang chủ',
          tabBarIcon: ({ color }) => (
            <Ionicons
              color={typeof color === 'string' ? color : '#94a3b8'}
              name="home-outline"
              size={24}
            />
          ),
        }}
      />
      <Tabs.Screen
        name="schedule"
        options={{
          title: 'Lịch học',
          tabBarIcon: ({ color }) => (
            <Feather
              name="calendar"
              size={24}
              color={typeof color === 'string' ? color : '#94a3b8'}
            />
          ),
        }}
      />
      <Tabs.Screen
        name="notifications"
        options={{
          title: 'Thông báo',
          tabBarBadge: unreadCount > 0 ? (unreadCount > 99 ? '99+' : unreadCount) : undefined,
          tabBarIcon: ({ color }) => (
            <Ionicons
              color={typeof color === 'string' ? color : '#94a3b8'}
              name="notifications-outline"
              size={24}
            />
          ),
        }}
      />
      <Tabs.Screen
        name="progress"
        options={{
          title: 'Tiến độ',
          tabBarIcon: ({ color }) => (
            <Feather
              name="bar-chart-2"
              size={24}
              color={typeof color === 'string' ? color : '#94a3b8'}
            />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Cá nhân',
          tabBarIcon: ({ color }) => (
            <Ionicons
              color={typeof color === 'string' ? color : '#94a3b8'}
              name="person-outline"
              size={24}
            />
          ),
        }}
      />
    </Tabs>
  );
}