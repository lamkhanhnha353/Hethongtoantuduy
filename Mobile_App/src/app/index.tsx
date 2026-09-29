import {StyleSheet , View, Text, TouchableOpacity} from 'react-native';
import { router } from 'expo-router';

export default function ScreenHome() {
  return (
    <View style={styles.container}>
        <Text style={styles.title}>Hello world</Text>

        <TouchableOpacity
           style={styles.button}
           onPress={ () => router.push('/login')  }
        >
          <Text style={styles.textbutton}>Bấm vào đây hãy</Text> 

        </TouchableOpacity>
    </View>
  )
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#fff',
  }, 
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 20,
  },
  button: {
    backgroundColor: 'red',
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 8,
  },
  textbutton: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  }
})
