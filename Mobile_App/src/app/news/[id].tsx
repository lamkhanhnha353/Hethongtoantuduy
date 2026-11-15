import { Feather } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Image, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

type Article = {
  id: string;
  categoryLabel: string;
  title: string;
  author: string;
  date: string;
  readTime: string;
  imageUrl: string;
  content: { type: 'lead' | 'h2' | 'p' | 'tip' | 'list'; text?: string; items?: string[] }[];
  tags: string[];
};

const ARTICLE_DATA: Record<string, Article> = {
  '1': {
    id: '1',
    categoryLabel: 'Cẩm nang',
    title: 'Phương pháp kích hoạt não phải cho bé 5–7 tuổi',
    author: 'Chuyên gia Toán Trí Tuệ',
    date: '12 Thg 11, 2026',
    readTime: '5 phút đọc',
    imageUrl: 'https://images.unsplash.com/photo-1503454537195-1dcabb73ffb9?auto=format&fit=crop&q=80&w=800',
    tags: ['Não bộ', 'Flash Anzan', 'Tư duy hình ảnh'],
    content: [
      {
        type: 'lead',
        text: 'Giai đoạn 5–7 tuổi được các nhà khoa học gọi là "cửa sổ vàng" của não bộ — thời điểm não phải đang phát triển mạnh nhất và dễ tiếp thu nhất các kỹ năng tư duy hình ảnh, tính toán trực giác và trí nhớ không gian.',
      },
      {
        type: 'h2',
        text: 'Tại sao não phải quan trọng trong toán học?',
      },
      {
        type: 'p',
        text: 'Não phải đảm nhận tư duy hình ảnh, cảm xúc và trí tưởng tượng. Khi bé học Soroban hay Finger Math đúng cách, não phải sẽ hình thành "bàn tính ảo trong đầu" — giúp bé tính nhẩm siêu nhanh mà không cần dụng cụ hỗ trợ.',
      },
      {
        type: 'h2',
        text: '5 bài tập Flash Anzan tại nhà',
      },
      {
        type: 'list',
        items: [
          'Chớp số 2 chữ số (1 giây/số): Bố mẹ gọi to 5 số ngẫu nhiên, bé nhẩm cộng và nói kết quả.',
          'Nhìn & nhớ bàn tính: Chỉ 1 ô trên bàn tính trong 2 giây, bé đóng mắt và tưởng tượng lại.',
          'Cộng ngón tay mắt nhắm: Bé thực hiện phép tính bằng ngón tay trong khi nhắm mắt.',
          'Đếm ngược từ 50: Mỗi tối đếm ngược 50→1 bằng ngón tay để rèn phản xạ.',
          'Flash Card số: Bố mẹ giơ nhanh thẻ số (< 1 giây), bé nói ngay số vừa thấy.',
        ],
      },
      {
        type: 'tip',
        text: 'Mẹo nhỏ: Chỉ cần 10 phút mỗi ngày, không ép bé làm nhiều hơn. Sự kiên nhẫn và đều đặn quan trọng hơn thời lượng.',
      },
      {
        type: 'p',
        text: 'Hãy nhớ: mục tiêu không phải là bé làm đúng ngay lập tức, mà là xây dựng thói quen tư duy. Sau 3–4 tuần luyện đều đặn, bạn sẽ thấy sự khác biệt rõ rệt trong tốc độ phản xạ của con.',
      },
    ],
  },
  '2': {
    id: '2',
    categoryLabel: 'Sự kiện',
    title: 'Kỳ thi chứng chỉ Soroban Quốc tế tháng 12/2026',
    author: 'Ban tổ chức',
    date: '10 Thg 11, 2026',
    readTime: '2 phút đọc',
    imageUrl: 'https://images.unsplash.com/photo-1523050854058-8df90110c9f1?auto=format&fit=crop&q=80&w=800',
    tags: ['Chứng chỉ', 'Thi quốc tế', 'Tháng 12'],
    content: [
      {
        type: 'lead',
        text: 'Trung tâm Toán Trí Tuệ thông báo lịch thi cấp chứng chỉ Soroban Quốc tế kỳ tháng 12/2026 dành cho tất cả học viên đủ điều kiện.',
      },
      {
        type: 'h2',
        text: 'Thông tin kỳ thi',
      },
      {
        type: 'list',
        items: [
          'Thời gian thi: Thứ Bảy, ngày 20/12/2026 (8:00 – 11:30)',
          'Địa điểm: Cơ sở chính – Trung tâm Toán Trí Tuệ',
          'Đối tượng: Học viên từ Cấp 6 trở lên',
          'Hạn đăng ký: 25/11/2026',
        ],
      },
      {
        type: 'tip',
        text: 'Để đăng ký, phụ huynh vui lòng liên hệ giáo viên chủ nhiệm hoặc nhắn tin trực tiếp qua mục Nhắn tin trong ứng dụng.',
      },
    ],
  }
};

const DEFAULT_ARTICLE: Omit<Article, 'id'> = {
  categoryLabel: 'Tin tức',
  title: 'Nội dung đang được cập nhật',
  author: 'Trung tâm Toán Trí Tuệ',
  date: 'Tháng 11, 2026',
  readTime: '3 phút đọc',
  imageUrl: 'https://images.unsplash.com/photo-1503454537195-1dcabb73ffb9?auto=format&fit=crop&q=80&w=800',
  tags: ['Toán Trí Tuệ'],
  content: [
    {
      type: 'lead',
      text: 'Nội dung chi tiết của bài viết này đang được biên tập. Vui lòng quay lại sau.',
    },
  ],
};

export default function NewsDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();

  const article: Article = ARTICLE_DATA[id ?? ''] ?? { id: id ?? '', ...DEFAULT_ARTICLE };

  return (
    <SafeAreaView className="flex-1 bg-white" edges={['top']}>
      
      {/* 
        Nút Quay lại - Đã đổi thành icon chevron-left (<) thay cho mũi tên 
        và đẩy top xuống thấp hơn (top-16 thay vì top-12) để tránh lẹm màn hình
      */}
      <View className="absolute top-16 left-4 z-10 bg-white/90 p-2.5 rounded-full shadow-sm">
        <TouchableOpacity activeOpacity={0.7} onPress={() => router.back()}>
          <Feather name="chevron-left" size={26} color="#1e293b" />
        </TouchableOpacity>
      </View>

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 60 }}>
        
        {/* Ảnh Cover Header */}
        <Image 
          source={{ uri: article.imageUrl }} 
          className="w-full h-80 bg-slate-200"
          resizeMode="cover"
        />

        <View className="px-5 -mt-8 pt-8 bg-white rounded-t-3xl shadow-sm">
          
          {/* Label & Date */}
          <View className="flex-row items-center justify-between mb-4">
            <View className="bg-sky-50 px-3 py-1.5 rounded-md">
              <Text className="text-[11px] font-bold text-sky-700 uppercase tracking-widest">
                {article.categoryLabel}
              </Text>
            </View>
            <Text className="text-sm text-slate-500 font-medium">{article.date}</Text>
          </View>

          {/* Tiêu đề */}
          <Text className="text-3xl font-black text-slate-800 leading-tight mb-6">
            {article.title}
          </Text>

          {/* Tác giả & Thời gian */}
          <View className="flex-row items-center justify-between py-4 border-y border-slate-100 mb-6">
            <View className="flex-row items-center gap-3">
              <View className="w-10 h-10 rounded-full bg-slate-100 items-center justify-center">
                <Feather name="user" size={18} color="#64748b" />
              </View>
              <View>
                <Text className="text-sm font-bold text-slate-700">{article.author}</Text>
                <Text className="text-xs text-slate-500">Tác giả bài viết</Text>
              </View>
            </View>
            <View className="flex-row items-center gap-1.5">
              <Feather name="clock" size={14} color="#94a3b8" />
              <Text className="text-sm font-medium text-slate-500">{article.readTime}</Text>
            </View>
          </View>

          {/* Nội dung bài viết */}
          <View className="gap-5">
            {article.content.map((block, index) => {
              if (block.type === 'lead') {
                return (
                  <Text key={index} className="text-[17px] text-slate-700 leading-relaxed font-semibold">
                    {block.text}
                  </Text>
                );
              }
              if (block.type === 'h2') {
                return (
                  <Text key={index} className="text-xl font-bold text-slate-900 mt-4">
                    {block.text}
                  </Text>
                );
              }
              if (block.type === 'p') {
                return (
                  <Text key={index} className="text-base text-slate-600 leading-loose">
                    {block.text}
                  </Text>
                );
              }
              if (block.type === 'tip') {
                return (
                  <View key={index} className="bg-slate-50 border-l-4 border-slate-800 p-4 rounded-r-xl my-2">
                    <View className="flex-row items-center gap-2 mb-1">
                      <Feather name="info" size={16} color="#1e293b" />
                      <Text className="font-bold text-slate-800">Lưu ý nhỏ</Text>
                    </View>
                    <Text className="text-base text-slate-700 leading-relaxed">{block.text}</Text>
                  </View>
                );
              }
              if (block.type === 'list' && block.items) {
                return (
                  <View key={index} className="gap-3 my-2">
                    {block.items.map((item, i) => (
                      <View key={i} className="flex-row items-start gap-3">
                        <View className="w-1.5 h-1.5 rounded-full bg-slate-400 mt-2.5 flex-shrink-0" />
                        <Text className="flex-1 text-base text-slate-600 leading-loose">{item}</Text>
                      </View>
                    ))}
                  </View>
                );
              }
              return null;
            })}

            {/* Tags */}
            <View className="flex-row flex-wrap gap-2 mt-8 pt-6 border-t border-slate-100">
              {article.tags.map((tag) => (
                <View key={tag} className="bg-slate-100 px-4 py-2 rounded-full">
                  <Text className="text-xs text-slate-500 font-bold uppercase tracking-wider">
                    {tag}
                  </Text>
                </View>
              ))}
            </View>

          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}