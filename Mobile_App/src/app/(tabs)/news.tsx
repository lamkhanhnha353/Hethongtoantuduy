import { useState } from 'react';
import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import {
  Image,
  ScrollView,
  Text,
  TouchableOpacity,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

type Category = 'all' | 'news' | 'tips' | 'events';

type Article = {
  id: string;
  category: Exclude<Category, 'all'>;
  categoryLabel: string;
  title: string;
  summary: string;
  author: string;
  date: string;
  readTime: string;
  imageUrl: string;
  featured?: boolean;
};

// Dữ liệu mẫu (Đã thay đổi sang dùng hình ảnh minh họa tĩnh để giao diện chuyên nghiệp hơn emoji)
const ARTICLES: Article[] = [
  {
    id: '1',
    featured: true,
    category: 'tips',
    categoryLabel: 'Cẩm nang',
    title: 'Phương pháp kích hoạt não phải cho bé 5–7 tuổi',
    summary: 'Khám phá "cửa sổ vàng" phát triển não bộ và 5 bài tập Flash Anzan đơn giản giúp bé tăng cường trí nhớ không gian ngay tại nhà.',
    author: 'Chuyên gia Toán Trí Tuệ',
    date: '12 Thg 11, 2026',
    readTime: '5 phút',
    imageUrl: 'https://images.unsplash.com/photo-1503454537195-1dcabb73ffb9?auto=format&fit=crop&q=80&w=800',
  },
  {
    id: '2',
    category: 'events',
    categoryLabel: 'Sự kiện',
    title: 'Kỳ thi chứng chỉ Soroban Quốc tế tháng 12/2026',
    summary: 'Thông báo lịch thi cấp chứng chỉ Soroban quốc tế dành cho học viên từ Cấp 6 trở lên. Hạn chót đăng ký: 25/11/2026.',
    author: 'Ban tổ chức',
    date: '10 Thg 11, 2026',
    readTime: '2 phút',
    imageUrl: 'https://images.unsplash.com/photo-1523050854058-8df90110c9f1?auto=format&fit=crop&q=80&w=800',
  },
  {
    id: '3',
    category: 'tips',
    categoryLabel: 'Cẩm nang',
    title: '10 mẹo đồng hành cùng con luyện Finger Math',
    summary: 'Chỉ cần 10 phút mỗi ngày với những bài luyện ngón tay đơn giản, bé sẽ hình thành phản xạ tính nhẩm bền vững.',
    author: 'Cô Minh Thư',
    date: '08 Thg 11, 2026',
    readTime: '4 phút',
    imageUrl: 'https://images.unsplash.com/photo-1606092195730-5d7b9af1efc5?auto=format&fit=crop&q=80&w=800',
  },
  {
    id: '4',
    category: 'news',
    categoryLabel: 'Tin tức',
    title: 'Vinh danh học viên xuất sắc tháng 11',
    summary: 'Chúc mừng 12 bé đã đạt thành tích nổi bật trong tháng 11 với kết quả bài kiểm tra vượt mức kỳ vọng.',
    author: 'Ban giám đốc',
    date: '05 Thg 11, 2026',
    readTime: '3 phút',
    imageUrl: 'https://images.unsplash.com/photo-1532012197267-da84d127e765?auto=format&fit=crop&q=80&w=800',
  },
];

const CATEGORIES: { key: Category; label: string }[] = [
  { key: 'all', label: 'Tất cả' },
  { key: 'news', label: 'Tin tức' },
  { key: 'tips', label: 'Cẩm nang' },
  { key: 'events', label: 'Sự kiện' },
];

export default function NewsScreen() {
  const router = useRouter();
  const [activeCategory, setActiveCategory] = useState<Category>('all');

  const featured = ARTICLES.find((a) => a.featured)!;
  const filtered =
    activeCategory === 'all'
      ? ARTICLES.filter((a) => !a.featured)
      : ARTICLES.filter((a) => a.category === activeCategory && !a.featured);

  const handlePress = (id: string) => {
    router.push({ pathname: '/news/[id]', params: { id } });
  };

  return (
    <SafeAreaView className="flex-1 bg-[#5a64f1]" edges={['top']}>
      {/* Header màu xanh đồng bộ với trang Lịch học */}
      <View className="px-5 pt-4 pb-5 bg-[#5a64f1]">
        <Text className="text-2xl font-black text-white">Góc Phụ Huynh</Text>
        <Text className="text-sm font-medium text-indigo-100 mt-1">Khám phá cẩm nang & tin tức mới nhất</Text>
      </View>

      <View className="flex-1 bg-slate-50">
        <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 40 }}>

          {/* Bộ lọc danh mục (Tabs) */}
          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={{ paddingHorizontal: 20, gap: 12, paddingVertical: 16 }}
          >
            {CATEGORIES.map((cat) => (
              <TouchableOpacity
                key={cat.key}
                activeOpacity={0.7}
                onPress={() => setActiveCategory(cat.key)}
                className={`px-5 py-2.5 rounded-full ${
                  activeCategory === cat.key
                    ? 'bg-slate-800'
                    : 'bg-white border border-slate-200'
                }`}
              >
                <Text
                  className={`text-sm font-bold ${
                    activeCategory === cat.key ? 'text-white' : 'text-slate-600'
                  }`}
                >
                  {cat.label}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>

          {/* Bài viết Nổi bật (Featured) */}
          {activeCategory === 'all' && (
            <TouchableOpacity
              activeOpacity={0.9}
              className="mx-5 mb-8 bg-white rounded-3xl overflow-hidden shadow-sm border border-slate-100"
              onPress={() => handlePress(featured.id)}
            >
              <Image
                source={{ uri: featured.imageUrl }}
                className="w-full h-48 bg-slate-200"
                resizeMode="cover"
              />
              <View className="p-5">
                <View className="flex-row items-center gap-2 mb-3">
                  <View className="bg-sky-100 px-3 py-1 rounded-md">
                    <Text className="text-sky-700 text-xs font-bold uppercase tracking-wider">
                      {featured.categoryLabel}
                    </Text>
                  </View>
                  <Text className="text-xs text-slate-400 font-medium">{featured.date}</Text>
                </View>

                <Text className="text-xl font-bold text-slate-800 leading-tight mb-2">
                  {featured.title}
                </Text>
                <Text className="text-sm text-slate-500 leading-relaxed mb-4" numberOfLines={2}>
                  {featured.summary}
                </Text>

                <View className="flex-row items-center justify-between pt-4 border-t border-slate-100">
                  <Text className="text-sm font-medium text-slate-600">{featured.author}</Text>
                  <View className="flex-row items-center gap-1.5">
                    <Feather name="clock" size={14} color="#64748b" />
                    <Text className="text-sm text-slate-500">{featured.readTime}</Text>
                  </View>
                </View>
              </View>
            </TouchableOpacity>
          )}

          {/* Danh sách bài viết nhỏ */}
          <View className="px-5 gap-4">
            <Text className="text-lg font-bold text-slate-800 mb-1">Mới cập nhật</Text>

            {filtered.map((article) => (
              <TouchableOpacity
                key={article.id}
                activeOpacity={0.85}
                onPress={() => handlePress(article.id)}
                className="bg-white rounded-2xl p-4 flex-row gap-4 border border-slate-100 shadow-sm"
              >
                <Image
                  source={{ uri: article.imageUrl }}
                  className="w-24 h-24 rounded-xl bg-slate-200"
                  resizeMode="cover"
                />
                <View className="flex-1 justify-between py-1">
                  <View>
                    <View className="flex-row items-center gap-2 mb-1.5">
                      <Text className="text-[10px] font-bold text-sky-600 uppercase tracking-widest">
                        {article.categoryLabel}
                      </Text>
                      <Text className="text-[10px] text-slate-400">{article.date}</Text>
                    </View>
                    <Text className="font-bold text-slate-800 text-base leading-snug mb-1" numberOfLines={2}>
                      {article.title}
                    </Text>
                  </View>
                  <View className="flex-row items-center gap-1">
                    <Feather name="clock" size={12} color="#94a3b8" />
                    <Text className="text-xs text-slate-500">{article.readTime}</Text>
                  </View>
                </View>
              </TouchableOpacity>
            ))}

            {filtered.length === 0 && (
              <Text className="text-center text-slate-400 mt-8">Chưa có bài viết nào trong danh mục này.</Text>
            )}
          </View>
        </ScrollView>
      </View>
    </SafeAreaView>
  );
}