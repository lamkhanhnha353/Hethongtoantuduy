import { forwardRef, type ReactNode } from 'react';
import { Text, TextInput, View, type TextInputProps } from 'react-native';

export type FormFieldProps = TextInputProps & {
  label: string;
  error?: string;
  hint?: string;
  rightAccessory?: ReactNode;
  containerClassName?: string;
};

export const FormField = forwardRef<TextInput, FormFieldProps>(function FormField(
  { label, error, hint, rightAccessory, className, containerClassName, ...inputProps },
  ref,
) {
  return (
    <View className={containerClassName ?? 'mb-4'}>
      <Text className="mb-1.5 text-sm font-medium text-slate-700">{label}</Text>

      <View className="relative justify-center">
        <TextInput
          ref={ref}
          placeholderTextColor="#94a3b8"
          className={`rounded-xl border bg-white px-4 py-3 text-base text-slate-900 ${
            rightAccessory ? 'pr-20' : ''
          } ${error ? 'border-red-500' : 'border-slate-300'} ${className ?? ''}`}
          {...inputProps}
        />

        {rightAccessory ? <View className="absolute right-3">{rightAccessory}</View> : null}
      </View>

      {error ? <Text className="mt-1 text-xs text-red-600">{error}</Text> : null}
      {!error && hint ? <Text className="mt-1 text-xs text-slate-500">{hint}</Text> : null}
    </View>
  );
});