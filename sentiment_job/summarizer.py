"""
Résumé ultra-court via GPT-4o-mini.
Renvoie <= 30 mots, aucune opinion (« juste les faits »).
"""
import backoff, os
from openai import OpenAI, OpenAIError
import tiktoken
from dotenv import load_dotenv
load_dotenv()
client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

enc = tiktoken.encoding_for_model("gpt-4o-mini")
MAX_MODEL_TOKENS = 128_000
RESERVED_FOR_PROMPT = 2_000  # header + réponse
MAX_CONTEXT_TOKENS = MAX_MODEL_TOKENS - RESERVED_FOR_PROMPT



@backoff.on_exception(backoff.expo, OpenAIError, max_tries=5)
def summarize(text: str, avg_score: float) -> str:

    tokens = enc.encode(text)
    if len(tokens) > MAX_CONTEXT_TOKENS:
        tokens = tokens[:MAX_CONTEXT_TOKENS]
        text = enc.decode(tokens)

    trend = ("bullish (favorable)" if avg_score > 0.55
             else "bearish (défavorable)" if avg_score < 0.45
    else "neutre")

    prompt = f"""
        You are a crypto analyst. In 25 words maximum, explain
        WHY this topic is {trend}.

        Context:
        \"\"\"{text}\"\"\"

        Respond in English, no frills.
        """

    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.3,
    )
    return resp.choices[0].message.content.strip()