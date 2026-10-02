import { forwardRef, useState } from 'react';
import { Text, TextInput, TouchableOpacity } from 'react-native';

import { FormField, type FormFieldProps } from '@/components/FormField';

type PasswordFieldProps = Omit<FormFieldProps, 'secureTextEntry' | 'rightAccessory'>;

export const PasswordField = forwardRef<TextInput, PasswordFieldProps>(
  function PasswordField({ label = 'Mật khẩu', ...inputProps }, ref) {
    const [isVisible, setIsVisible] = useState(false);

    return (
      <FormField
        {...inputProps}
        label={label}
        ref={ref}
        secureTextEntry={!isVisible}
        rightAccessory={
          <TouchableOpacity
            accessibilityLabel={isVisible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
            accessibilityRole="button"
            hitSlop={8}
            onPress={() => setIsVisible((previous) => !previous)}
          >
            <Text className="text-xs font-semibold text-sky-700">
              {isVisible ? 'Ẩn' : 'Hiện'}
            </Text>
          </TouchableOpacity>
        }
      />
    );
  },
);