import { useState } from 'react';
import { View, Text, TouchableOpacity, Animated, Alert } from 'react-native';
import { router } from 'expo-router';
import { Feather, MaterialCommunityIcons } from '@expo/vector-icons';

export default function VerifyPinScreen() {
  const [pin, setPin] = useState<string>('');
  const [isError, setIsError] = useState(false);
  
  
  const [shakeAnim] = useState(() => new Animated.Value(0));

 
  const triggerShake = () => {
    shakeAnim.setValue(0); 
    Animated.sequence([
      Animated.timing(shakeAnim, { toValue: 10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: -10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: 10, duration: 50, useNativeDriver: true }),
      Animated.timing(shakeAnim, { toValue: 0, duration: 50, useNativeDriver: true })
    ]).start();
  };

  
  const handlePress = (num: string) => {
    if (isError) setIsError(false);
    
    if (pin.length < 4) {
      const newPin = pin + num;
      setPin(newPin);

     
      if (newPin.length === 4) {
        
        setTimeout(() => {
          if (newPin === '1234') {
            router.replace('/parent');
          } else {
            setIsError(true);
            triggerShake();
            setPin(''); 
          }
        }, 50);
      }
    }
  };

  const handleDelete = () => {
    if (isError) setIsError(false);
    setPin((prev) => prev.slice(0, -1));
  };


  const renderKey = (num: string) => (
    <TouchableOpacity
      key={num}
      activeOpacity={0.5} 
      onPress={() => handlePress(num)}
      className="w-[80px] h-16 rounded-2xl bg-white border border-slate-200 shadow-sm flex items-center justify-center"
    >
      <Text className="text-2xl font-semibold text-slate-800">{num}</Text>
    </TouchableOpacity>
  );

  return (
    <View className="flex-1 bg-[#f8fafc]">
     
      <View className="bg-indigo-600 pt-16 pb-8 relative overflow-hidden">
        <View className="absolute -top-12 -right-12 w-40 h-40 rounded-full bg-cyan-400/20 opacity-50" />
        <View className="absolute bottom-0 left-4 w-24 h-24 rounded-full bg-indigo-300/20 opacity-50" />
        
        <View className="px-5 flex-row items-center gap-3 relative z-10">
          <TouchableOpacity 
            activeOpacity={0.5} 
            onPress={() => router.back()}
            className="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center border border-white/30"
          >
            <Feather name="chevron-left" size={24} color="white" />
          </TouchableOpacity>
          <View>
            <Text className="text-sm text-indigo-100">Xác thực bảo mật</Text>
            <Text className="text-lg font-bold text-white leading-tight">Nhập mã PIN</Text>
          </View>
        </View>
      </View>

     
      <View className="flex-1 items-center px-6 pt-8 pb-5">
        
     
        <View className="items-center mb-8">
          <View className="relative mb-3">
            <View className="w-20 h-20 rounded-full bg-indigo-500 flex items-center justify-center border-4 border-white shadow-lg">
              <Text className="text-white font-bold text-2xl">MA</Text>
            </View>
            <View className="absolute -bottom-1 -right-1 w-8 h-8 rounded-full bg-indigo-600 border-[3px] border-white flex items-center justify-center">
              <Feather name="lock" size={14} color="white" />
            </View>
          </View>
          <Text className="font-bold text-slate-900 text-lg">Nguyễn Minh Anh</Text>
          <Text className="text-sm text-slate-500 mt-0.5">Phụ huynh</Text>
        </View>

        <Text className="text-sm text-slate-600 font-medium mb-6">
          Nhập mã PIN để truy cập không gian quản lý
        </Text>

     
        <Animated.View style={{ transform: [{ translateX: shakeAnim }] }} className="flex-row items-center gap-5 mb-2">
          {[0, 1, 2, 3].map((index) => {
            const isFilled = index < pin.length;
            return (
              <View 
                key={index} 
                className={`w-[18px] h-[18px] rounded-full border-[2.5px] ${
                  isError 
                    ? 'border-rose-500 bg-rose-500' 
                    : isFilled 
                      ? 'bg-indigo-600 border-indigo-600 scale-110' 
                      : 'border-slate-300 bg-transparent'
                }`} 
              />
            );
          })}
        </Animated.View>

     
        <View className="h-6 mb-6 justify-center">
          {isError && (
            <Text className="text-sm text-rose-500 font-medium text-center">
              Sai mã PIN. Vui lòng thử lại
            </Text>
          )}
        </View>

     
        <View className="w-[264px] flex-row flex-wrap justify-between gap-y-3">
       
          {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map(renderKey)}
          
      
          <TouchableOpacity 
            activeOpacity={0.5}
            onPress={() => Alert.alert("Thông báo", "Tính năng đang phát triển")}
            className="w-[80px] h-16 rounded-2xl bg-indigo-50 border border-indigo-100 flex items-center justify-center shadow-sm"
          >
            <MaterialCommunityIcons name="face-recognition" size={28} color="#4f46e5" />
          </TouchableOpacity>
          
          {renderKey('0')}
          
          <TouchableOpacity 
            activeOpacity={0.5}
            onPress={handleDelete}
            disabled={pin.length === 0}
            className="w-[80px] h-16 rounded-2xl bg-white border border-slate-200 flex items-center justify-center shadow-sm"
          >
            <Feather name="delete" size={24} color="#64748b" />
          </TouchableOpacity>
        </View>

       
        <TouchableOpacity className="mt-8 pt-2">
          <Text className="text-indigo-600 text-sm font-semibold">Quên mã PIN?</Text>
        </TouchableOpacity>

      </View>
    </View>
  );
}