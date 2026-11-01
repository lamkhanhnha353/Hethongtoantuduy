import React from 'react';
import { Canvas, Circle, RadialGradient, vec, Text as SkiaText, matchFont } from '@shopify/react-native-skia';
import { Platform } from 'react-native';

type NumberBubbleProps = {
    number?: number;
    x?: number;
    y?: number;
    radius?: number;
    size?: number;
};

export const NumberBubble = ({
    number = 5,
    x = 100,
    y = 100,
    radius = 40,
    size = 200,
}: NumberBubbleProps) => {
    const fontFamily = Platform.select({ ios: 'Helvetica', default: 'sans-serif' });
    const font = matchFont({ fontFamily, fontSize: 36, fontWeight: 'bold' });

    return (
        <Canvas style={{ width: size, height: size }}>
            <Circle cx={x} cy={y} r={radius}>
                <RadialGradient
                    c={vec(x - 15, y - 15)}
                    r={radius}
                    colors={['#FF9A9E', '#FECFEF', '#FF6B6B']}
                />
            </Circle>

            {font && (
                <SkiaText
                    x={x - 10}
                    y={y + 12}
                    text={String(number)}
                    font={font}
                    color="white"
                />
            )}
        </Canvas>
    );
};