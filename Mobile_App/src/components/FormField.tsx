import { forwardRef } from 'react';
import { Text, TextInput, View, type TextInputProps } from 'react-native';

type FormFieldProps = TextInputProps & {
  label: string;
  error?: string;
};

export const FormField = forwardRef<TextInput, FormFieldProps>(function FormField(
  { label, error, className, ...inputProps },
  ref,
) {
  return (
    <View className="mb-4">
      <Text className="mb-1.5 text-sm font-medium text-slate-700">{label}</Text>
      <TextInput
        ref={ref}
        placeholderTextColor="#94a3b8"
        className={`rounded-xl border bg-white px-4 py-3 text-base text-slate-900 ${
          error ? 'border-red-500' : 'border-slate-300'
        } ${className ?? ''}`}
        {...inputProps}
      />
      {error ? <Text className="mt-1 text-xs text-red-600">{error}</Text> : null}
    </View>
  );
});
