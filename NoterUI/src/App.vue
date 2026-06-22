<script setup>
import { ref, onMounted } from "vue";
import { createStudyGoal, getStudyGoals } from "./api/goals";
import { createMilestone, getMilestones, updateMilestoneStatus } from "./api/milestones";

const goals = ref([]);
const errorMsg = ref("");
const successMsg = ref("");
const isLoadingGoals = ref(false);
const isCreatingGoal = ref(false);
const isCreatingMilestoneByGoal = ref({});
const isUpdatingMilestoneById = ref({});

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

async function loadGoals() {
  isLoadingGoals.value = true;
  errorMsg.value = "";

  try {
    goals.value = await getStudyGoals();
  } catch (err) {
    errorMsg.value = "Studienziele konnten nicht geladen werden: " + (err.response?.status || err.message);
  } finally {
    isLoadingGoals.value = false;
  }
}

onMounted(loadGoals);

const toggleMilestones = async (goalId) => {
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

  if (!goalForm.value.userId.trim()) {
    errorMsg.value = "Bitte eine gueltige User-ID eingeben.";
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
    milestoneFormByGoal.value[goalId] = { title: "" };
  }
};

const submitMilestone = async (goalId) => {
  errorMsg.value = "";
  successMsg.value = "";
  ensureMilestoneForm(goalId);

  const title = milestoneFormByGoal.value[goalId].title?.trim();
  if (!title) {
    errorMsg.value = "Bitte einen Titel fuer den Meilenstein eingeben.";
    return;
  }

  isCreatingMilestoneByGoal.value = {
    ...isCreatingMilestoneByGoal.value,
    [goalId]: true
  };

  try {
    await createMilestone({
      studyGoalId: goalId,
      title
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
  return currentStatus;
};

const updateStatusForMilestone = async (milestone) => {
  errorMsg.value = "";
  successMsg.value = "";

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
</script>

<template>
  <main class="container">
    <header class="hero panel">
      <p class="eyebrow">Lernzeit Manager</p>
      <h1>Lernziele strukturiert planen</h1>
      <p class="lead">
        Erstelle Studienziele, zerlege sie in Meilensteine und behalte deinen Fortschritt auf jedem Gerät im Blick.
      </p>
    </header>

    <section class="panel">
      <div class="section-head">
        <h2>Neues Studienziel erstellen</h2>
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
          />
        </label>
      </div>

      <button class="primary" :disabled="isCreatingGoal" @click="submitGoal">
        {{ isCreatingGoal ? "Speichern..." : "Studienziel erstellen" }}
      </button>
    </section>

    <p v-if="successMsg" class="success-box">{{ successMsg }}</p>
    <p v-if="errorMsg" class="error-box">{{ errorMsg }}</p>

    <section class="panel">
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
        <h3>{{ g.title }}</h3>
        <p class="desc">{{ g.description }}</p>

        <div class="actions">
          <button class="ghost" @click="toggleMilestones(g.id)">
            {{ openGoals[g.id] ? "Meilensteine verbergen" : "Meilensteine anzeigen" }}
          </button>
        </div>

        <div v-if="openGoals[g.id]" class="milestone-wrap">
          <ul v-if="milestonesByGoal[g.id]" class="milestone-list">
            <li v-for="m in milestonesByGoal[g.id]" :key="m.id" class="milestone">
              <span class="title">{{ m.title }}</span>
              <button
                type="button"
                class="status"
                :class="{
                  planned: m.status === 0,
                  progress: m.status === 1,
                  done: m.status === 2,
                  failed: m.status === 3
                }"
                :disabled="isUpdatingMilestoneById[m.id] || m.status === 2"
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

  .milestone {
    flex-direction: column;
    align-items: flex-start;
  }

  .inline-form {
    flex-direction: column;
    align-items: stretch;
  }

  .primary,
  .ghost {
    width: 100%;
  }
}
</style>