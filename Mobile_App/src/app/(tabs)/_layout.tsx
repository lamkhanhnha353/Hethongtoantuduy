import { Feather, Ionicons } from '@expo/vector-icons';
import { Tabs, router } from 'expo-router';
import { TouchableOpacity } from 'react-native';

import { useSession } from '@/hooks/useSession';

export default function TabsLayout() {
  const { session } = useSession();
  const isParent = session?.role === 'Parent';

  return (
    <Tabs
      screenOptions={{
        headerLeft: () => (
          <TouchableOpacity
            accessibilityLabel="Quay lại chọn hồ sơ"
            accessibilityRole="button"
            className="ml-2 h-9 w-9 items-center justify-center rounded-full border border-slate-200 bg-white"
            hitSlop={8}
            onPress={() => router.replace('/select-profile')}
          >
            <Ionicons color="#334155" name="arrow-back" size={18} />
          </TouchableOpacity>
        ),
        headerShadowVisible: false,
        headerStyle: { backgroundColor: '#ffffff' },
        headerTitle: '',
        tabBarActiveTintColor: '#0284c7',
        tabBarInactiveTintColor: '#94a3b8',
        tabBarStyle: { borderTopColor: '#e2e8f0' },
      }}
    >
      <Tabs.Screen
        name="home"
        options={{
          title: 'Trang chủ',
          tabBarIcon: ({ color, size }) => (
            <Ionicons color={color} name="home-outline" size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="students"
        options={{
          title: 'Học sinh',
          href: isParent ? '/students' : null,
          tabBarIcon: ({ color, size }) => (
            <Ionicons color={color} name="people-outline" size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: 'Cá nhân',
          tabBarIcon: ({ color, size }) => (
            <Ionicons color={color} name="person-outline" size={size} />
          ),
        }}
      />
      <Tabs.Screen
        name="schedule"
        options={{
          title: 'Lịch học',
          tabBarIcon: ({ color, size }) => <Feather name="calendar" size={24} color={color} />,
        }}
      />
      <Tabs.Screen
        name="progress"
        options={{
          title: 'Tiến độ',
          tabBarIcon: ({ color, size }) => <Feather name="bar-chart-2" size={24} color={color} />,
        }}
      />
    </Tabs>
  );
}
