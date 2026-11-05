from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import ollama

app = FastAPI(title="AI RAG Backend Service", version="1.0")

# Khai báo model cấu hình sẵn của bạn
LLM_MODEL = "qwen2.5:3b-instruct"
EMBEDDING_MODEL = "nomic-embed-text"

class ChatRequest(BaseModel):
    prompt: str

@app.get("/")
def health_check():
    return {"status": "AI Service is running", "model": LLM_MODEL}

@app.post("/api/chat")
def chat_with_ai(request: ChatRequest):
    try:
        # Gọi trực tiếp Ollama Python client
        response = ollama.chat(model=LLM_MODEL, messages=[
            {
                'role': 'user',
                'content': request.prompt,
            },
        ])
        
        return {
            "success": True,
            "answer": response['message']['content']
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    # Chạy server tại cổng 8000
    uvicorn.run("main:app", host="127.0.0.1", port=8000, reload=True)