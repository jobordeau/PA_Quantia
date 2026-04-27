from __future__ import annotations
import datetime as dt, functools, logging, httpx, time

log  = logging.getLogger(__name__)
BASE = "https://api.coingecko.com/api/v3/"

def _get_json_retry(url: str, max_tries: int = 5) -> dict:
    delay = 2.0
    for i in range(max_tries):
        try:
            r = httpx.get(url, timeout=20)
            if r.status_code == 429:             
                raise httpx.HTTPStatusError(
                    "429 Too Many Requests", request=r.request, response=r
                )
            r.raise_for_status()
            return r.json()

        except httpx.HTTPStatusError as exc:
            if exc.response.status_code == 429 and i < max_tries - 1:
                log.warning("CoinGecko 429 → pause %.0f s (try %d/%d)",
                            delay, i+1, max_tries)
                time.sleep(delay)
                delay *= 2
                continue
            raise        

@functools.lru_cache(maxsize=256)
def fetch_price(symbol: str, ts: dt.datetime) -> float | None:

    ts = ts.replace(tzinfo=dt.timezone.utc)
    unix_from = int((ts - dt.timedelta(minutes=30)).timestamp())
    unix_to   = int((ts + dt.timedelta(minutes=30)).timestamp())

    url = (f"{BASE}coins/{symbol}/market_chart/range"
           f"?vs_currency=usd&from={unix_from}&to={unix_to}")
    try:
        data = _get_json_retry(url)
        prices = [p[1] for p in data["prices"]]           
        return round(float(sum(prices) / len(prices)), 2)
    except Exception as exc:                              
        log.warning("CoinGecko error: %s → None", exc)
        return None
