(() => {
  "use strict";

  const PLAYER_ID_KEY = "clickerTycoonPlayerId";
  let playerId = null;
  let refreshInFlight = false;

  const el = {
    resourceValue: document.getElementById("resourceValue"),
    stageName: document.getElementById("stageName"),
    clickValue: document.getElementById("clickValue"),
    critChance: document.getElementById("critChance"),
    autoRate: document.getElementById("autoRate"),
    activeEffects: document.getElementById("activeEffects"),
    crisisBanner: document.getElementById("crisisBanner"),
    crisisCount: document.getElementById("crisisCount"),
    clickButton: document.getElementById("clickButton"),
    floatingLayer: document.getElementById("floatingLayer"),
    nextStageLabel: document.getElementById("nextStageLabel"),
    stageProgressPercent: document.getElementById("stageProgressPercent"),
    stageProgressFill: document.getElementById("stageProgressFill"),
    stageDescription: document.getElementById("stageDescription"),
    upgradesList: document.getElementById("upgradesList"),
    monetizationList: document.getElementById("monetizationList"),
    footerStage: document.getElementById("footerStage"),
    toastContainer: document.getElementById("toastContainer"),
    resetButton: document.getElementById("resetButton"),
  };

  // ---------- helpers ----------

  function formatNumber(value) {
    const n = Number(value) || 0;
    const abs = Math.abs(n);
    if (abs < 1000) return Math.round(n).toString();

    const units = ["K", "M", "B", "T", "Qa", "Qi"];
    let unitIndex = -1;
    let v = n;
    while (Math.abs(v) >= 1000 && unitIndex < units.length - 1) {
      v /= 1000;
      unitIndex++;
    }
    const decimals = Math.abs(v) < 10 ? 2 : Math.abs(v) < 100 ? 1 : 0;
    return v.toFixed(decimals) + units[unitIndex];
  }

  function toast(message, kind = "info") {
    const div = document.createElement("div");
    div.className = "toast" + (kind === "error" ? " error" : "");
    div.textContent = message;
    el.toastContainer.appendChild(div);
    setTimeout(() => div.remove(), 4500);
  }

  async function apiRequest(url, method) {
    let res;
    try {
      res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
      });
    } catch (networkError) {
      throw new Error("Нет соединения с сервером. Убедитесь, что backend запущен.");
    }

    if (!res.ok) {
      let message = `Ошибка сервера (${res.status})`;
      try {
        const body = await res.json();
        if (body && body.error) message = body.error;
      } catch {
        // response wasn't JSON - keep generic message
      }
      throw new Error(message);
    }

    return res.json();
  }

  const getJson = (url) => apiRequest(url, "GET");
  const postJson = (url) => apiRequest(url, "POST");

  // ---------- rendering ----------

  function render(state) {
    el.resourceValue.textContent = formatNumber(state.resource);
    el.stageName.textContent = state.stageName;
    el.clickValue.textContent = "+" + formatNumber(state.clickValuePreview);
    el.critChance.textContent = state.critChancePercent + "%";
    el.autoRate.textContent = formatNumber(state.automationRatePerSecond) + " / сек";
    el.footerStage.textContent = state.currentStage;
    el.stageDescription.textContent = state.stageDescription;

    renderActiveEffects(state.activeEffects, state.crisisActionsRemaining);
    renderStageProgress(state);
    renderUpgrades(state);
    renderMonetization(state);
  }

  function renderActiveEffects(effects, crisisRemaining) {
    el.activeEffects.innerHTML = "";
    effects.forEach((effect) => {
      const chip = document.createElement("span");
      chip.className = "effect-chip";
      chip.textContent = `${effect.label} · ${effect.secondsRemaining}с · x${effect.multiplier.toFixed(1)}`;
      el.activeEffects.appendChild(chip);
    });

    if (crisisRemaining > 0) {
      el.crisisBanner.classList.remove("hidden");
      el.crisisCount.textContent = crisisRemaining;
    } else {
      el.crisisBanner.classList.add("hidden");
    }
  }

  function renderStageProgress(state) {
    if (!state.nextStage) {
      el.nextStageLabel.textContent = "Максимальный этап достигнут";
      el.stageProgressPercent.textContent = "100%";
      el.stageProgressFill.style.width = "100%";
      return;
    }

    const next = state.nextStage;
    const percent = Math.max(0, Math.min(100, (state.resource / next.resourceRequired) * 100 || 0));
    const upgradePart = next.requiredUpgradeName ? ` + улучшение «${next.requiredUpgradeName}»` : "";

    el.nextStageLabel.textContent = `Дальше: ${next.name} (${formatNumber(next.resourceRequired)} ресурса${upgradePart})`;
    el.stageProgressPercent.textContent = Math.floor(percent) + "%";
    el.stageProgressFill.style.width = percent + "%";
  }

  function renderUpgrades(state) {
    el.upgradesList.innerHTML = "";

    state.upgrades.forEach((u) => {
      const card = document.createElement("div");
      card.className = "upgrade-card" + (u.unlocked ? "" : " locked");

      const canAfford = state.resource >= u.nextCost;
      const buttonLabel = u.maxedOut ? "MAX" : !u.unlocked ? `Этап ${u.requiredStage}` : formatNumber(u.nextCost);

      card.innerHTML = `
        <div class="upgrade-icon">${u.icon}</div>
        <div class="upgrade-info">
          <h3>${u.name}${u.changesMechanic ? '<span class="mechanic-tag">меняет механику</span>' : ""}</h3>
          <p>${u.description}</p>
          <div class="upgrade-meta">Уровень ${u.level}${u.maxLevel < 999 ? "/" + u.maxLevel : ""} · ${u.effectSummary}</div>
        </div>
        <button class="buy-button" ${u.maxedOut || !u.unlocked || !canAfford ? "disabled" : ""}>${buttonLabel}</button>
      `;

      card.querySelector("button").addEventListener("click", () => buyUpgrade(u.id));
      el.upgradesList.appendChild(card);
    });
  }

  function renderMonetization(state) {
    el.monetizationList.innerHTML = "";

    const icons = { ad: "📺", premium: "📷", "starter-pack": "🎁" };

    state.monetizationOffers.forEach((offer) => {
      const card = document.createElement("div");
      card.className = "offer-card";

      let buttonLabel = "Купить (симуляция)";
      let disabled = false;

      if (offer.kind === "ad") {
        buttonLabel = offer.active ? `${offer.secondsRemaining}с` : "Посмотреть рекламу";
        disabled = offer.active;
      } else {
        buttonLabel = offer.purchased ? "Куплено" : "Купить (симуляция)";
        disabled = offer.purchased;
      }

      if (disabled) card.classList.add("disabled");

      card.innerHTML = `
        <div class="offer-icon">${icons[offer.kind] || "🛍️"}</div>
        <div class="offer-info">
          <h3>${offer.name}</h3>
          <p>${offer.description}</p>
        </div>
        <button class="offer-button" ${disabled ? "disabled" : ""}>${buttonLabel}</button>
      `;

      card.querySelector("button").addEventListener("click", () => activateOffer(offer.id));
      el.monetizationList.appendChild(card);
    });
  }

  function spawnFloatingNumber(amount, isCritical) {
    const div = document.createElement("div");
    div.className = "floating-number" + (isCritical ? " crit" : "");
    div.textContent = (isCritical ? "CRITICAL! " : "") + "+" + formatNumber(amount);
    div.style.left = 50 + (Math.random() * 30 - 15) + "%";
    el.floatingLayer.appendChild(div);
    setTimeout(() => div.remove(), 900);
  }

  // ---------- actions ----------

  async function handleClick() {
    try {
      const result = await postJson(`/api/game/${playerId}/click`);
      spawnFloatingNumber(result.amountGained, result.isCritical);

      if (result.triggeredEventMessage) {
        const kind = result.triggeredEventType === "viral" ? "info" : "error";
        toast(result.triggeredEventMessage, kind);
      }
      if (result.stageAdvanced) {
        toast(`Новый этап открыт: «${result.state.stageName}»!`, "info");
      }

      render(result.state);
    } catch (err) {
      toast(err.message, "error");
    }
  }

  async function buyUpgrade(upgradeId) {
    try {
      const result = await postJson(`/api/game/${playerId}/upgrades/${upgradeId}`);
      render(result.state);
    } catch (err) {
      toast(err.message, "error");
    }
  }

  async function activateOffer(offerId) {
    try {
      const result = await postJson(`/api/game/${playerId}/monetization/${offerId}`);
      toast(result.message, "info");
      render(result.state);
    } catch (err) {
      toast(err.message, "error");
    }
  }

  async function resetProgress() {
    const confirmed = window.confirm(
      "Точно сбросить весь прогресс? Ресурс, улучшения и этап будут обнулены. Это необратимо."
    );
    if (!confirmed) return;

    el.resetButton.disabled = true;
    try {
      const state = await postJson(`/api/game/${playerId}/reset`);
      render(state);
      toast("Прогресс сброшен. Начинаем сначала!", "info");
    } catch (err) {
      toast(err.message, "error");
    } finally {
      el.resetButton.disabled = false;
    }
  }

  async function refresh() {
    if (refreshInFlight) return;
    refreshInFlight = true;
    try {
      const state = await getJson(`/api/game/${playerId}`);
      render(state);
    } catch (err) {
      if (err.message && err.message.includes("не найден")) {
        // Save was lost server-side (e.g. DB reset during development) - start a fresh one.
        localStorage.removeItem(PLAYER_ID_KEY);
        location.reload();
        return;
      }
      toast(err.message, "error");
    } finally {
      refreshInFlight = false;
    }
  }

  // ---------- bootstrap ----------

  async function init() {
    let id = localStorage.getItem(PLAYER_ID_KEY);

    if (!id) {
      try {
        const data = await postJson("/api/players");
        id = data.playerId;
        localStorage.setItem(PLAYER_ID_KEY, id);
      } catch (err) {
        toast(err.message, "error");
        return;
      }
    }

    playerId = id;
    el.clickButton.addEventListener("click", handleClick);
    el.resetButton.addEventListener("click", resetProgress);

    await refresh();
    setInterval(refresh, 2500);
  }

  init();
})();
