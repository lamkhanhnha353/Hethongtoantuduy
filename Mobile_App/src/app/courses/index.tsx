import { useState, useMemo, useEffect } from 'react';
import { View, Text, FlatList, TouchableOpacity, Pressable, TextInput, ScrollView, Modal, ActivityIndicator, Alert } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';

import { CourseCard, CourseType } from '@/components/courses/CourseCard';
import { API_URL } from '@/config/env';

const QUICK_FILTERS = [
  { id: 'all', label: 'Tất cả' },
  { id: 'fm', label: 'Finger Math' },
  { id: 'sb', label: 'Soroban' },
  { id: 'lg', label: 'Toán Logic' },
  { id: 'open', label: 'Đang chiêu sinh' }
];

const ITEMS_PER_PAGE = 4;

function getCourseType(code: string) {
  const normalizedCode = code.toUpperCase();

  if (normalizedCode.includes('FGM') || normalizedCode.includes('FM')) return 'fm';
  if (normalizedCode.includes('SRB') || normalizedCode.includes('SB')) return 'sb';
  if (['TL', 'LG', 'MTN', 'KMN', 'IMT', 'CPA'].some(typeCode => normalizedCode.includes(typeCode))) {
    return 'lg';
  }

  return normalizedCode.toLowerCase();
}

export default function AllCoursesScreen() {
  const router = useRouter();
  const insets = useSafeAreaInsets();
  
  const [query, setQuery] = useState('');
  const [activeFilter, setActiveFilter] = useState('all');
  const [sortAsc, setSortAsc] = useState(true);
  const [showVoiceModal, setShowVoiceModal] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [coursesData, setCoursesData] = useState<CourseType[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const handleQueryChange = (value: string) => {
    setQuery(value);
    setCurrentPage(1);
  };

  const handleFilterChange = (filter: string) => {
    setActiveFilter(filter);
    setCurrentPage(1);
  };

  const handleSortToggle = () => {
    setSortAsc(current => !current);
    setCurrentPage(1);
  };

  // ĐƯA HÀM LẤY DỮ LIỆU VÀO TRONG USEEFFECT ĐỂ HẾT GẠCH ĐỎ
  useEffect(() => {
    const fetchData = async () => {
      try {
        const response = await fetch(`${API_URL}/api/courses`); 
        if (!response.ok) throw new Error(`Mã lỗi HTTP: ${response.status}`);

        const data = await response.json();
        const formattedCourses: CourseType[] = [];

        data.forEach((course: any) => {
          const codeCheck = String(course.code || '').toUpperCase();
          const courseType = getCourseType(codeCheck);
          const colorClass = courseType === 'fm'
            ? 'bg-indigo-600'
            : courseType === 'sb'
              ? 'bg-sky-500'
              : courseType === 'lg'
                ? 'bg-blue-500'
                : 'bg-slate-500';

          if (course.courseLevels && Array.isArray(course.courseLevels)) {
            course.courseLevels.forEach((level: any) => {
              formattedCourses.push({
                id: level.id,
                code: course.code || '',
                name: level.name || '',
                minAge: course.minAge || 0, 
                maxAge: course.maxAge || 0,
                duration: `${Math.ceil((level.totalSessions || 0) / 8)} tháng`, 
                price: level.fee || 0,
                color: colorClass,
                type: courseType,
                status: ['open', 'active'].includes(String(level.status || '').toLowerCase()) ? 'open' : 'soon'
              });
            });
          }
        });

        setCoursesData(formattedCourses);
      } catch (error) {
        console.error("Lỗi:", error);
        Alert.alert("Lỗi", "Không thể lấy dữ liệu khóa học.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchData();
  }, []);

  const filteredCourses = useMemo(() => {
    return coursesData.filter(course => {
      if (query && !course.name.toLowerCase().includes(query.toLowerCase())) return false;
      
      if (['fm', 'sb', 'lg'].includes(activeFilter) && course.type !== activeFilter) return false;
      if (activeFilter === 'open' && course.status !== 'open') return false;
      
      return true;
    }).sort((a, b) => sortAsc ? a.price - b.price : b.price - a.price);
  }, [coursesData, query, activeFilter, sortAsc]);

  const totalPages = Math.ceil(filteredCourses.length / ITEMS_PER_PAGE);
  const paginatedCourses = filteredCourses.slice(
    (currentPage - 1) * ITEMS_PER_PAGE, 
    currentPage * ITEMS_PER_PAGE
  );

  return (
    <View className="flex-1 bg-[#f8fafc]">
      <View className="bg-white px-4 pb-4 rounded-b-[32px] shadow-sm shadow-slate-200/50 z-10" style={{ paddingTop: insets.top + 8 }}>
        <View className="flex-row items-center justify-between mb-5">
          <View className="flex-row items-center gap-3">
            <TouchableOpacity onPress={() => router.back()} className="w-10 h-10 rounded-full bg-slate-50 border border-slate-100 items-center justify-center">
              <Feather name="arrow-left" size={22} color="#334155" />
            </TouchableOpacity>
            <View>
              <Text className="text-slate-900 text-xl font-extrabold">Khóa học</Text>
              <Text className="text-slate-500 text-[12px] font-medium mt-0.5">Chương trình học tiêu chuẩn</Text>
            </View>
          </View>
        </View>

        <View className="flex-row gap-2.5">
          <View className="flex-1 relative justify-center">
            <Feather name="search" size={18} color="#94a3b8" className="absolute left-4 z-10" />
            <TextInput
              value={query}
              onChangeText={handleQueryChange}
              placeholder="Tìm khóa học, độ tuổi…"
              placeholderTextColor="#94a3b8"
              className="w-full bg-slate-100 border border-slate-200 text-slate-900 rounded-2xl py-3 pl-11 pr-10 text-[15px] font-medium"
            />
            {query.length > 0 && (
              <TouchableOpacity onPress={() => handleQueryChange('')} className="absolute right-3 z-10 p-2">
                <Feather name="x-circle" size={18} color="#94a3b8" />
              </TouchableOpacity>
            )}
          </View>
          <TouchableOpacity onPress={() => setShowVoiceModal(true)} className="w-12 h-12 bg-indigo-50 border border-indigo-100 rounded-2xl items-center justify-center">
            <Feather name="mic" size={20} color="#4f46e5" />
          </TouchableOpacity>
        </View>

        <ScrollView horizontal showsHorizontalScrollIndicator={false} className="mt-4">
          <View className="flex-row gap-2.5 pr-4">
            {QUICK_FILTERS.map(filter => {
              const isActive = activeFilter === filter.id;
              return (
                <Pressable
                  key={filter.id}
                  onPress={() => handleFilterChange(filter.id)}
                  style={{
                    paddingHorizontal: 16,
                    paddingVertical: 8,
                    borderRadius: 12,
                    borderWidth: 1,
                    backgroundColor: isActive ? '#4f46e5' : '#f8fafc',
                    borderColor: isActive ? '#4f46e5' : '#e2e8f0',
                    ...(isActive
                      ? {
                          shadowColor: '#c7d2fe',
                          shadowOffset: { width: 0, height: 1 },
                          shadowOpacity: 0.25,
                          shadowRadius: 2,
                          elevation: 2,
                        }
                      : {}),
                  }}
                >
                  <Text style={{ fontSize: 13, fontWeight: '700', color: isActive ? '#ffffff' : '#475569' }}>
                    {filter.label}
                  </Text>
                </Pressable>
              );
            })}
          </View>
        </ScrollView>
      </View>

      <View className="flex-row items-center justify-between px-5 py-4">
        <Text className="text-[13px] font-semibold text-slate-500">
          Có <Text className="text-indigo-600 font-extrabold">{filteredCourses.length}</Text> khóa học phù hợp
        </Text>
        <TouchableOpacity onPress={handleSortToggle} className="flex-row items-center gap-1.5 bg-white px-3 py-1.5 rounded-lg border border-slate-200">
          <Feather name="bar-chart-2" size={14} color="#4f46e5" />
          <Text className="text-[12px] font-bold text-indigo-600">Học phí {sortAsc ? '↑' : '↓'}</Text>
        </TouchableOpacity>
      </View>

      {isLoading ? (
        <View className="flex-1 items-center justify-center pb-20">
          <ActivityIndicator size="large" color="#4f46e5" />
        </View>
      ) : (
        <FlatList
          data={paginatedCourses}
          keyExtractor={item => item.id ? item.id.toString() : Math.random().toString()}
          contentContainerStyle={{ paddingHorizontal: 16, paddingBottom: 20 }}
          showsVerticalScrollIndicator={false}
          renderItem={({ item }) => (
            <CourseCard 
              course={item} 
              onPress={() => router.push(`/courses/${item.id}`)} 
            />
          )}
          
          /* ĐÃ PHỤC HỒI LẠI GIAO DIỆN PHÂN TRANG (Pagination) */
          ListFooterComponent={() => (
            totalPages > 1 ? (
              <View className="flex-row items-center justify-center gap-6 py-6 mb-8">
                <TouchableOpacity 
                  disabled={currentPage === 1}
                  onPress={() => setCurrentPage(prev => prev - 1)}
                  className={`w-10 h-10 rounded-full items-center justify-center border ${currentPage === 1 ? 'bg-slate-50 border-slate-100' : 'bg-white border-slate-200 shadow-sm'}`}
                >
                  <Feather name="chevron-left" size={20} color={currentPage === 1 ? '#cbd5e1' : '#4f46e5'} />
                </TouchableOpacity>
                <Text className="text-sm font-bold text-slate-700">
                  Trang <Text className="text-indigo-600">{currentPage}</Text> / {totalPages}
                </Text>
                <TouchableOpacity 
                  disabled={currentPage === totalPages}
                  onPress={() => setCurrentPage(prev => prev + 1)}
                  className={`w-10 h-10 rounded-full items-center justify-center border ${currentPage === totalPages ? 'bg-slate-50 border-slate-100' : 'bg-white border-slate-200 shadow-sm'}`}
                >
                  <Feather name="chevron-right" size={20} color={currentPage === totalPages ? '#cbd5e1' : '#4f46e5'} />
                </TouchableOpacity>
              </View>
            ) : null
          )}

          /* ĐÃ PHỤC HỒI LẠI GIAO DIỆN KHI TRỐNG (Empty State) */
          ListEmptyComponent={() => (
            <View className="items-center justify-center py-16">
              <View className="w-16 h-16 rounded-full bg-slate-100 items-center justify-center mb-4">
                <Feather name="search" size={28} color="#94a3b8" />
              </View>
              <Text className="text-[15px] font-bold text-slate-700">Không tìm thấy khóa học</Text>
              <Text className="text-[13px] text-slate-500 mt-1">Vui lòng thử từ khóa hoặc bộ lọc khác</Text>
            </View>
          )}
        />
      )}

      <Modal visible={showVoiceModal} transparent animationType="fade">
        <View className="flex-1 bg-slate-900/60 justify-end">
          <View className="bg-white rounded-t-[32px] px-6 pt-5 pb-10 items-center">
            <View className="w-12 h-1.5 bg-slate-200 rounded-full mb-6" />
            <Text className="text-[15px] font-bold text-slate-800">Đang nghe...</Text>
            <Text className="text-[13px] text-slate-500 mt-1 mb-8">Nói tên khóa học VD: Soroban</Text>
            <View className="w-20 h-20 rounded-full bg-indigo-600 items-center justify-center shadow-lg shadow-indigo-500/50 mb-8 mt-4">
              <Feather name="mic" size={32} color="white" />
            </View>
            <TouchableOpacity onPress={() => setShowVoiceModal(false)} className="w-full py-4 rounded-xl bg-slate-100 items-center">
              <Text className="text-[15px] font-bold text-slate-700">Đóng</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>

    </View>
  );
}