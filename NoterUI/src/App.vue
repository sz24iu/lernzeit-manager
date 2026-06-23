<script setup>
import { computed, ref, onMounted, onUnmounted, watch } from "vue";
import { login } from "./api/auth";
import { setAccessToken } from "./api/http";
import { createStudyGoal, getStudyGoals } from "./api/goals";
import { createMilestone, getMilestones, updateMilestoneStatus } from "./api/milestones";

const tokenStorageKey = "noter.jwt";
const demoUserEmail = "demo.lernende@noter.local";
const demoUserPassword = "Test123!";

const goals = ref([]);
const errorMsg = ref("");
const successMsg = ref("");
const isLoadingGoals = ref(false);
const isCreatingGoal = ref(false);
const isCreatingMilestoneByGoal = ref({});
const isUpdatingMilestoneById = ref({});
const isAuthenticating = ref(false);
const authToken = ref(localStorage.getItem(tokenStorageKey) || "");
const currentUserId = ref("");
const authForm = ref({
  email: demoUserEmail,
  password: demoUserPassword
});
const isAuthenticated = computed(() => Boolean(authToken.value));

// Timer-State
const timerSeconds = ref(0);
const timerRunning = ref(false);
const timerStopped = ref(false);
let timerInterval = null;

const goalForm = ref({
  title: "",
  description: "",
  type: 0,
  startDate: new Date().toISOString().slice(0, 10),
  endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
  userId: ""
});

const milestoneFormByGoal = ref({});
const milestonesByGoal = ref({});
const openGoals = ref({});

setAccessToken(authToken.value);

watch(authToken, (value) => {
  setAccessToken(value);
  if (!value) {
    currentUserId.value = "";
    goalForm.value.userId = "";
    goals.value = [];
    milestonesByGoal.value = {};
    openGoals.value = {};
  }
});

async function loadGoals() {
  if (!isAuthenticated.value) {
    goals.value = [];
    return;
  }

  isLoadingGoals.value = true;
  errorMsg.value = "";

  try {
    goals.value = await getStudyGoals();
    if (!goalForm.value.userId && currentUserId.value) {
      goalForm.value.userId = currentUserId.value;
    }
  } catch (err) {
    errorMsg.value = "Studienziele konnten nicht geladen werden: " + (err.response?.status || err.message);
  } finally {
    isLoadingGoals.value = false;
  }
}

function persistToken(token) {
  authToken.value = token;
  setAccessToken(token);

  if (token) {
    localStorage.setItem(tokenStorageKey, token);
    return;
  }

  localStorage.removeItem(tokenStorageKey);
}

function ensureAuthenticated(actionLabel) {
  if (isAuthenticated.value) {
    return true;
  }

  errorMsg.value = `${actionLabel} erfordert eine Anmeldung mit dem Test-User.`;
  return false;
}

async function submitLogin() {
  errorMsg.value = "";
  successMsg.value = "";
  isAuthenticating.value = true;

  try {
    const result = await login({
      email: authForm.value.email.trim(),
      password: authForm.value.password
    });

    if (!result.success || !result.token) {
      throw new Error(result.errors?.join(", ") || "Login fehlgeschlagen.");
    }

    persistToken(result.token);
    currentUserId.value = decodeUserId(result.token);
    if (currentUserId.value) {
      goalForm.value.userId = currentUserId.value;
    }
    successMsg.value = `Angemeldet als ${authForm.value.email.trim()}.`;
    await loadGoals();
  } catch (err) {
    errorMsg.value = "Anmeldung fehlgeschlagen: " + (err.response?.data?.errors?.join(", ") || err.message);
  } finally {
    isAuthenticating.value = false;
  }
}

function logout() {
  persistToken("");
  successMsg.value = "Abgemeldet.";
  errorMsg.value = "";
}

function decodeUserId(token) {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const payload = JSON.parse(atob(padded));
    return payload.sub || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || "";
  } catch {
    return "";
  }
}

// Timer-Funktionen
const formatTimer = (seconds) => {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = seconds % 60;
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
};

const startTimer = () => {
  if (!timerRunning.value && !timerStopped.value) {
    timerRunning.value = true;
    timerInterval = setInterval(() => {
      timerSeconds.value += 1;
    }, 1000);
  }
};

const pauseTimer = () => {
  if (timerRunning.value) {
    timerRunning.value = false;
    clearInterval(timerInterval);
  }
};

const stopTimer = () => {
  timerRunning.value = false;
  timerStopped.value = true;
  clearInterval(timerInterval);
};

const resetTimer = () => {
  timerRunning.value = false;
  timerStopped.value = false;
  timerSeconds.value = 0;
  clearInterval(timerInterval);
};

onMounted(loadGoals);

onUnmounted(() => {
  if (timerInterval) {
    clearInterval(timerInterval);
  }
});

const toggleMilestones = async (goalId) => {
  if (!openGoals.value[goalId] && !ensureAuthenticated("Das Laden von Meilensteinen")) {
    return;
  }

  openGoals.value[goalId] = !openGoals.value[goalId];

  if (!openGoals.value[goalId]) {
    return;
  }

  if (milestonesByGoal.value[goalId]) {
    return;
  }

  try {
    const data = await getMilestones(goalId);
    milestonesByGoal.value[goalId] = data;
  } catch (err) {
    errorMsg.value = "Meilensteine konnten nicht geladen werden: " + (err.response?.status || err.message);
  }
};

const submitGoal = async () => {
  errorMsg.value = "";
  successMsg.value = "";

  if (!isAuthenticated.value) {
    errorMsg.value = "Bitte zuerst anmelden.";
    return;
  }

  if (!goalForm.value.userId.trim()) {
    errorMsg.value = "Bitte eine gueltige User-ID eingeben.";
    return;
  }

  if (!goalForm.value.title.trim() || !goalForm.value.description.trim()) {
    errorMsg.value = "Titel und Beschreibung duerfen nicht leer sein.";
    return;
  }

  if (goalForm.value.endDate < goalForm.value.startDate) {
    errorMsg.value = "Das Enddatum darf nicht vor dem Startdatum liegen.";
    return;
  }

  isCreatingGoal.value = true;

  try {
    await createStudyGoal({
      title: goalForm.value.title,
      description: goalForm.value.description,
      type: Number(goalForm.value.type),
      startDate: new Date(goalForm.value.startDate).toISOString(),
      endDate: new Date(goalForm.value.endDate).toISOString(),
      userId: goalForm.value.userId.trim()
    });

    successMsg.value = "Studienziel wurde erstellt.";
    goalForm.value.title = "";
    goalForm.value.description = "";
    await loadGoals();
  } catch (err) {
    errorMsg.value = "Studienziel konnte nicht erstellt werden: " + (err.response?.status || err.message);
  } finally {
    isCreatingGoal.value = false;
  }
};

const ensureMilestoneForm = (goalId) => {
  if (!milestoneFormByGoal.value[goalId]) {
    milestoneFormByGoal.value[goalId] = {
      title: "",
      dueDate: new Date().toISOString().slice(0, 10),
      dueTime: new Date().toTimeString().slice(0, 5)
    };
  }
};

const submitMilestone = async (goalId) => {
  errorMsg.value = "";
  successMsg.value = "";

  if (!ensureAuthenticated("Das Erstellen von Meilensteinen")) {
    return;
  }

  ensureMilestoneForm(goalId);

  const title = milestoneFormByGoal.value[goalId].title?.trim();
  if (!title) {
    errorMsg.value = "Bitte einen Titel fuer den Meilenstein eingeben.";
    return;
  }

  const dueDate = milestoneFormByGoal.value[goalId].dueDate;
  const dueTime = milestoneFormByGoal.value[goalId].dueTime;
  if (!dueDate || !dueTime) {
    errorMsg.value = "Bitte Datum und Uhrzeit fuer den Meilenstein angeben.";
    return;
  }

  const dueDateTime = new Date(`${dueDate}T${dueTime}:00`).toISOString();

  isCreatingMilestoneByGoal.value = {
    ...isCreatingMilestoneByGoal.value,
    [goalId]: true
  };

  try {
    await createMilestone({
      studyGoalId: goalId,
      title,
      dueDateTime
    });

    milestoneFormByGoal.value[goalId].title = "";
    const data = await getMilestones(goalId);
    milestonesByGoal.value[goalId] = data;
    successMsg.value = "Meilenstein wurde erstellt.";
  } catch (err) {
    errorMsg.value = "Meilenstein konnte nicht erstellt werden: " + (err.response?.status || err.message);
  } finally {
    isCreatingMilestoneByGoal.value = {
      ...isCreatingMilestoneByGoal.value,
      [goalId]: false
    };
  }
};

const nextMilestoneStatus = (currentStatus) => {
  if (currentStatus === 0) return 1;
  if (currentStatus === 1) return 2;
  if (currentStatus === 2) return 0;
  return 0;
};

const updateStatusForMilestone = async (milestone) => {
  errorMsg.value = "";
  successMsg.value = "";

  if (!ensureAuthenticated("Das Aktualisieren des Status")) {
    return;
  }

  const currentStatus = Number(milestone.status);
  const nextStatus = nextMilestoneStatus(currentStatus);

  if (nextStatus === currentStatus) {
    return;
  }

  isUpdatingMilestoneById.value = {
    ...isUpdatingMilestoneById.value,
    [milestone.id]: true
  };

  try {
    await updateMilestoneStatus({
      id: milestone.id,
      status: nextStatus
    });

    milestone.status = nextStatus;
    successMsg.value = "Meilenstein-Status wurde aktualisiert.";
  } catch (err) {
    errorMsg.value = "Status konnte nicht aktualisiert werden: " + (err.response?.status || err.message);
  } finally {
    isUpdatingMilestoneById.value = {
      ...isUpdatingMilestoneById.value,
      [milestone.id]: false
    };
  }
};

const statusLabel = (status) => {
  if (status === 0) return "Geplant";
  if (status === 1) return "In Bearbeitung";
  if (status === 2) return "Abgeschlossen";
  return "Fehlgeschlagen";
};

const formatDueDateTime = (value) => {
  if (!value) return "Kein Termin gesetzt";
  const date = new Date(value);
  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date);
};

const typeLabel = (type) => {
  if (type === 0) return "Modul";
  if (type === 1) return "Klausur";
  if (type === 2) return "Projekt";
  if (type === 3) return "Aufgabe";
  return "Sonstiges";
};
</script>

<template>
  <main class="container">
    <header v-if="isAuthenticated" class="hero panel">
      <p class="eyebrow">Lernzeit Manager</p>
      <h1>Lernziele strukturiert planen</h1>
      <p class="lead">
        Erstelle Studienziele, zerlege sie in Meilensteine und behalte deinen Fortschritt auf jedem Gerät im Blick.
      </p>
    </header>

    <section v-if="!isAuthenticated" class="panel auth-panel">
      <div class="section-head">
        <div>
          <h2>Test-Login</h2>
          <p class="panel-note">
            Demo-Zugang: {{ demoUserEmail }} / {{ demoUserPassword }}
          </p>
        </div>
        <span class="badge" :class="isAuthenticated ? 'auth-active' : 'auth-idle'">
          {{ isAuthenticated ? "JWT aktiv" : "Nicht angemeldet" }}
        </span>
      </div>

      <div class="form-grid auth-grid">
        <label>
          E-Mail
          <input v-model="authForm.email" type="email" autocomplete="username" />
        </label>

        <label>
          Passwort
          <input v-model="authForm.password" type="password" autocomplete="current-password" />
        </label>
      </div>

      <div class="auth-actions">
        <button class="primary" :disabled="isAuthenticating" @click="submitLogin">
          {{ isAuthenticating ? "Anmeldung..." : "Mit Test-User anmelden" }}
        </button>
        <button class="ghost" :disabled="!isAuthenticated" @click="logout">
          Abmelden
        </button>
      </div>

      <p class="panel-note">
        Die geschuetzten Meilenstein-Endpunkte funktionieren erst nach erfolgreichem Login. Studienziele laden bleibt weiterhin offen.
      </p>
    </section>

    <section v-else class="panel auth-panel">
      <div class="section-head">
        <div>
          <h2>Angemeldet</h2>
          <p class="panel-note">Die Anwendung zeigt jetzt nur die Daten des aktuellen Nutzers.</p>
        </div>
        <button class="ghost" @click="logout">Abmelden</button>
      </div>
    </section>

    <section v-if="isAuthenticated" class="panel timer-panel">
      <div class="timer-header">
        <h2>Lernzeit-Stoppuhr</h2>
      </div>

      <div class="timer-display">
        <div class="timer-time">{{ formatTimer(timerSeconds) }}</div>
      </div>

      <div class="timer-controls">
        <button
          type="button"
          class="timer-btn start"
          :disabled="timerRunning || timerStopped"
          @click="startTimer"
        >
          ▶ Start
        </button>
        <button
          type="button"
          class="timer-btn pause"
          :disabled="!timerRunning"
          @click="pauseTimer"
        >
          ⏸ Pause
        </button>
        <button
          type="button"
          class="timer-btn stop"
          :disabled="timerStopped || timerSeconds === 0"
          @click="stopTimer"
        >
          ⏹ Stop
        </button>
        <button
          type="button"
          class="timer-btn reset"
          :disabled="timerSeconds === 0"
          @click="resetTimer"
        >
          ↺ Reset
        </button>
      </div>
    </section>

    <section v-if="isAuthenticated" class="panel">
      <div class="section-head">
        <h2>Neues Studienziel erstellen</h2>
        <span class="panel-note">Die User-ID wird, wenn moeglich, automatisch aus vorhandenen Demo-Zielen vorbelegt.</span>
      </div>

      <div class="form-grid">
        <label>
          Titel
          <input v-model="goalForm.title" type="text" placeholder="z.B. Statistik-Klausur bestehen" />
        </label>

        <label>
          Typ
          <select v-model="goalForm.type">
            <option :value="0">Modul</option>
            <option :value="1">Klausur</option>
            <option :value="2">Projekt</option>
            <option :value="3">Aufgabe</option>
            <option :value="4">Sonstiges</option>
          </select>
        </label>

        <label class="wide">
          Beschreibung
          <textarea v-model="goalForm.description" rows="3" placeholder="Kurzbeschreibung des Lernziels" />
        </label>

        <label>
          Startdatum
          <input v-model="goalForm.startDate" type="date" />
        </label>

        <label>
          Enddatum
          <input v-model="goalForm.endDate" type="date" />
        </label>

        <label class="wide">
          User-ID
          <input
            v-model="goalForm.userId"
            type="text"
            placeholder="GUID des Users, z.B. aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
            readonly
          />
        </label>
      </div>

      <button class="primary" :disabled="isCreatingGoal" @click="submitGoal">
        {{ isCreatingGoal ? "Speichern..." : "Studienziel erstellen" }}
      </button>
    </section>

    <p v-if="successMsg" class="success-box">{{ successMsg }}</p>
    <p v-if="errorMsg" class="error-box">{{ errorMsg }}</p>

    <section v-if="isAuthenticated" class="panel">
      <div class="section-head">
        <h2>Vorhandene Studienziele</h2>
        <button class="ghost" :disabled="isLoadingGoals" @click="loadGoals">
          {{ isLoadingGoals ? "Lade..." : "Neu laden" }}
        </button>
      </div>

      <p v-if="!isLoadingGoals && goals.length === 0" class="empty">
        Keine Ziele vorhanden. Erstelle oben dein erstes Studienziel.
      </p>

      <article v-for="g in goals" :key="g.id" class="card">
        <div class="card-header">
          <h3>{{ g.title }}</h3>
          <span class="badge type">{{ typeLabel(g.type) }}</span>
        </div>
        <p class="desc">{{ g.description }}</p>
        <p class="panel-note">Eigentümer: {{ g.userId }}</p>

        <div class="actions">
          <button class="ghost" @click="toggleMilestones(g.id)">
            {{ openGoals[g.id] ? "Meilensteine verbergen" : "Meilensteine anzeigen" }}
          </button>
        </div>

        <div v-if="openGoals[g.id]" class="milestone-wrap">
          <ul v-if="milestonesByGoal[g.id]" class="milestone-list">
            <li v-for="m in milestonesByGoal[g.id]" :key="m.id" class="milestone">
              <div class="milestone-info">
                <span class="title">{{ m.title }}</span>
                <span class="panel-note">Termin: {{ formatDueDateTime(m.dueDateTime) }}</span>
              </div>
              <button
                type="button"
                class="status"
                :class="{
                  planned: m.status === 0,
                  progress: m.status === 1,
                  done: m.status === 2,
                  failed: m.status === 3
                }"
                :disabled="isUpdatingMilestoneById[m.id]"
                @click="updateStatusForMilestone(m)"
              >
                {{ isUpdatingMilestoneById[m.id] ? "Speichern..." : statusLabel(m.status) }}
              </button>
            </li>
          </ul>
          <p v-else>Lade Meilensteine...</p>

          <div class="inline-form">
            <input
              :value="milestoneFormByGoal[g.id]?.title || ''"
              type="text"
              placeholder="Neuer Meilenstein"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].title = $event.target.value"
            />
            <input
              :value="milestoneFormByGoal[g.id]?.dueDate || ''"
              type="date"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].dueDate = $event.target.value"
            />
            <input
              :value="milestoneFormByGoal[g.id]?.dueTime || ''"
              type="time"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].dueTime = $event.target.value"
            />
            <button
              class="primary"
              :disabled="isCreatingMilestoneByGoal[g.id]"
              @click="submitMilestone(g.id)"
            >
              {{ isCreatingMilestoneByGoal[g.id] ? "Speichern..." : "Meilenstein erstellen" }}
            </button>
          </div>
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.container {
  width: min(1100px, 100%);
  margin: 0 auto;
  padding: clamp(16px, 4vw, 40px);
  color: #eef2ff;
  display: grid;
  gap: 16px;
}

.hero {
  padding: clamp(18px, 3vw, 28px);
  background:
    radial-gradient(circle at 85% 15%, rgba(106, 196, 255, 0.25), transparent 30%),
    radial-gradient(circle at 15% 10%, rgba(52, 211, 153, 0.2), transparent 32%),
    linear-gradient(140deg, #141a2b 0%, #101827 100%);
}

.eyebrow {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.14em;
  color: #9ec8ff;
  margin-bottom: 8px;
  font-weight: 700;
}

h1 {
  margin-bottom: 6px;
  font-size: clamp(1.6rem, 3.5vw, 2.3rem);
  line-height: 1.15;
  font-weight: 800;
}

.lead {
  color: #c6d4f8;
  max-width: 75ch;
}

h2 {
  margin-top: 0;
  font-size: 1.15rem;
  font-weight: 700;
}

.panel {
  background: linear-gradient(160deg, rgba(23, 31, 51, 0.94), rgba(15, 22, 38, 0.94));
  border: 1px solid #2d3b5f;
  border-radius: 16px;
  padding: clamp(14px, 2.6vw, 22px);
  box-shadow: 0 16px 42px rgba(5, 8, 16, 0.35);
}

.timer-panel {
  text-align: center;
}

.timer-header {
  margin-bottom: 14px;
}

.timer-display {
  background: rgba(10, 16, 29, 0.8);
  border: 2px solid #4a90c7;
  border-radius: 14px;
  padding: 24px;
  margin-bottom: 16px;
}

.timer-time {
  font-size: 4rem;
  font-weight: 800;
  font-family: "Courier New", monospace;
  color: #63b3ed;
  letter-spacing: 0.15em;
}

.timer-controls {
  display: flex;
  gap: 10px;
  justify-content: center;
  flex-wrap: wrap;
}

.timer-btn {
  padding: 10px 16px;
  border: 1px solid transparent;
  border-radius: 10px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.15s ease, filter 0.2s ease;
  font-size: 14px;
}

.timer-btn.start {
  background: linear-gradient(125deg, #10b981, #059669);
  color: white;
}

.timer-btn.pause {
  background: linear-gradient(125deg, #f59e0b, #d97706);
  color: white;
}

.timer-btn.stop {
  background: linear-gradient(125deg, #ef4444, #dc2626);
  color: white;
}

.timer-btn.reset {
  background: linear-gradient(125deg, #8b5cf6, #7c3aed);
  color: white;
}

.timer-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  filter: brightness(1.08);
}

.timer-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
}

.form-grid {
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-bottom: 14px;
}

.wide {
  grid-column: 1 / -1;
}

label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
  color: #d7e0fa;
}

input,
select,
textarea {
  border: 1px solid #3f5280;
  background: #0d1528;
  color: #f6f8ff;
  border-radius: 10px;
  padding: 10px 12px;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

input:focus,
select:focus,
textarea:focus {
  outline: none;
  border-color: #74b5ff;
  box-shadow: 0 0 0 3px rgba(72, 158, 255, 0.24);
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.card {
  border: 1px solid #33466f;
  border-radius: 14px;
  padding: 14px;
  margin-top: 10px;
  background: linear-gradient(170deg, rgba(22, 30, 48, 0.95), rgba(17, 24, 40, 0.95));
}

.desc {
  margin: 6px 0 12px;
  color: #c9d1e8;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  margin-bottom: 4px;
}

.card-header h3 {
  margin: 0;
  flex: 1;
}

.badge {
  padding: 4px 10px;
  border-radius: 8px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
}

.badge.type {
  background: rgba(99, 179, 237, 0.25);
  color: #63b3ed;
  border: 1px solid #4a90c7;
}

.badge.auth-active {
  background: rgba(16, 185, 129, 0.18);
  color: #6ee7b7;
  border: 1px solid #1f9d73;
}

.badge.auth-idle {
  background: rgba(148, 163, 184, 0.18);
  color: #d7e0fa;
  border: 1px solid #51627f;
}

.auth-panel {
  display: grid;
  gap: 14px;
}

.auth-grid {
  margin-bottom: 0;
}

.auth-actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.panel-note {
  margin: 4px 0 0;
  color: #aebedf;
  font-size: 0.95rem;
}

.actions {
  margin-bottom: 12px;
}

.milestone-wrap {
  margin-top: 8px;
}

.milestone-list {
  list-style: none;
  padding: 0;
  margin: 0 0 8px 0;
}

.milestone {
  display: flex;
  gap: 10px;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  border: 1px solid #2f3a52;
  border-radius: 10px;
  background: rgba(10, 16, 29, 0.45);
  margin-bottom: 6px;
}

.status {
  border: 1px solid transparent;
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: filter 0.2s ease, opacity 0.2s ease;
}

.status:hover {
  filter: brightness(1.08);
}

.status:disabled {
  cursor: not-allowed;
  opacity: 0.88;
}

.status.planned {
  background: #475569;
}

.status.progress {
  background: #f59e0b;
  color: #111827;
}

.status.done {
  background: #10b981;
  color: #052e24;
}

.status.failed {
  background: #ef4444;
}

.inline-form {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-top: 8px;
}

.inline-form input {
  flex: 1;
}

.primary,
.ghost {
  border-radius: 10px;
  padding: 10px 14px;
  border: 1px solid transparent;
  cursor: pointer;
  font-weight: 600;
  transition: transform 0.15s ease, filter 0.2s ease, border-color 0.2s ease;
}

.primary {
  background: linear-gradient(125deg, #3b82f6, #2563eb);
  color: white;
}

.ghost {
  background: rgba(137, 173, 240, 0.08);
  color: #dbe7ff;
  border-color: #496394;
}

.primary:hover,
.ghost:hover {
  transform: translateY(-1px);
  filter: brightness(1.05);
}

.primary:disabled,
.ghost:disabled {
  opacity: 0.62;
  cursor: not-allowed;
  transform: none;
}

.error-box,
.success-box,
.empty {
  border-radius: 10px;
  padding: 10px 12px;
  margin: 0;
}

.error-box {
  background: #482126;
  border: 1px solid #8d2d3d;
}

.success-box {
  background: #112f2a;
  border: 1px solid #1d7a59;
}

.empty {
  background: #15223b;
  border: 1px solid #27487a;
}

@media (max-width: 900px) {
  .section-head {
    flex-direction: column;
    align-items: flex-start;
  }
}

@media (max-width: 700px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .auth-actions {
    flex-direction: column;
  }

  .milestone {
    flex-direction: column;
    align-items: flex-start;
  }

  .inline-form {
    flex-direction: column;
    align-items: stretch;
  }

  .milestone-info {
    width: 100%;
  }

  .primary,
  .ghost {
    width: 100%;
  }
}
</style>