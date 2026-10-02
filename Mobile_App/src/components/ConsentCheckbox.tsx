import { Text, TouchableOpacity, View } from 'react-native';

type ConsentCheckboxProps = {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
};

export function ConsentCheckbox({ label, checked, onChange }: ConsentCheckboxProps) {
  return (
    <TouchableOpacity
      accessibilityRole="checkbox"
      accessibilityState={{ checked }}
      activeOpacity={0.9}
      className="mb-4 flex-row items-start gap-3"
      onPress={() => onChange(!checked)}
    >
      <View
        className={`mt-0.5 h-5 w-5 items-center justify-center rounded-md border ${
          checked ? 'border-sky-600 bg-sky-600' : 'border-slate-300 bg-white'
        }`}
      >
        {checked ? <Text className="text-xs font-bold text-white">✓</Text> : null}
      </View>

      <Text className="flex-1 text-xs leading-4 text-slate-600">{label}</Text>
    </TouchableOpacity>
  );
}