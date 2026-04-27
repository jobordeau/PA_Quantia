from __future__ import annotations

import datetime as dt
import logging
from typing import List, Optional

import httpx
import numpy as np

log = logging.getLogger(__name__)
BASE = "https://api.coingecko.com/api/v3"
LOOKBACK_DAYS = 30


def _fetch_market_chart(asset: str, days: int = LOOKBACK_DAYS) -> Optional[dict]:
    url = f"{BASE}/coins/{asset}/market_chart"
    params = {"vs_currency": "usd", "days": days}
    try:
        r = httpx.get(url, params=params, timeout=20)
        r.raise_for_status()
        return r.json()
    except Exception as exc:
        log.warning("CoinGecko market_chart error for %s: %s", asset, exc)
        return None


def _series_up_to(series: List[List[float]], cutoff_ms: int) -> List[float]:
    return [v for ts, v in series if ts <= cutoff_ms]


def _normalize(value: float, low: float, high: float) -> float:
    if high <= low:
        return 0.5
    return float(np.clip((value - low) / (high - low), 0.0, 1.0))


def _momentum_score(prices: List[float]) -> float:
    if len(prices) < 8:
        return 0.5
    recent = float(np.mean(prices[-7:]))
    base = float(np.mean(prices[-28:-7])) if len(prices) >= 28 else float(np.mean(prices[:-7]))
    if base == 0:
        return 0.5
    pct = (recent - base) / base
    return _normalize(pct, -0.20, 0.20)


def _volatility_score(prices: List[float]) -> float:
    if len(prices) < 8:
        return 0.5
    arr = np.asarray(prices, dtype=float)
    returns = np.diff(arr) / arr[:-1]
    if returns.size == 0:
        return 0.5
    vol = float(np.std(returns))
    inverted = 1.0 - _normalize(vol, 0.0, 0.05)
    return inverted


def _volume_score(volumes: List[float]) -> float:
    if len(volumes) < 8:
        return 0.5
    recent = float(np.mean(volumes[-7:]))
    base = float(np.mean(volumes[:-7]))
    if base == 0:
        return 0.5
    ratio = recent / base
    return _normalize(ratio, 0.5, 1.5)


def market_composite(ts_midnight: dt.datetime, asset: str = "bitcoin") -> float:
    chart = _fetch_market_chart(asset, LOOKBACK_DAYS)
    if not chart:
        return 0.5

    cutoff_ms = int(ts_midnight.replace(tzinfo=dt.timezone.utc).timestamp() * 1000)

    prices = _series_up_to(chart.get("prices", []), cutoff_ms)
    volumes = _series_up_to(chart.get("total_volumes", []), cutoff_ms)

    if not prices:
        return 0.5

    mom = _momentum_score(prices)
    vol = _volatility_score(prices)
    vlm = _volume_score(volumes)

    composite = 0.5 * mom + 0.25 * vol + 0.25 * vlm
    return round(float(np.clip(composite, 0.0, 1.0)), 3)
