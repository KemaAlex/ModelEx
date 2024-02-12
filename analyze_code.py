import openai
import os

def analyze_code(code):
    response = openai.Completion.create(
      engine="davinci-codex",
      prompt=code,
      temperature=0.5,
      max_tokens=100,
      top_p=1.0,
      frequency_penalty=0.0,
      presence_penalty=0.0
    )
    return response.choices[0].text.strip()

if __name__ == "__main__":
    openai.api_key = os.environ.get("OPENAI_API_KEY")
    
    # Здесь добавьте логику для чтения вашего кода, который нужно проанализировать
    code_to_analyze = "Тут должен быть ваш код для анализа"
    analysis_result = analyze_code(code_to_analyze)
    print("Результат анализа:", analysis_result)
