<script setup>
import { ref, onMounted } from "vue";
import { createStudyGoal, getStudyGoals } from "./api/goals";
import { createMilestone, getMilestones } from "./api/milestones";

const goals = ref([]);
const errorMsg = ref("");
const successMsg = ref("");
const isLoadingGoals = ref(false);
const isCreatingGoal = ref(false);
const isCreatingMilestoneByGoal = ref({});

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

const statusLabel = (status) => {
  if (status === 0) return "Geplant";
  if (status === 1) return "In Bearbeitung";
  if (status === 2) return "Abgeschlossen";
  return "Fehlgeschlagen";
};
</script>

<template>
  <div class="container">
    <h1>Lernziele</h1>

    <section class="panel">
      <h2>Neues Studienziel erstellen</h2>
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
              <span
                class="status"
                :class="{
                  planned: m.status === 0,
                  progress: m.status === 1,
                  done: m.status === 2,
                  failed: m.status === 3
                }"
              >
                {{ statusLabel(m.status) }}
              </span>
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
  </div>
</template>

<style scoped>
.container {
  max-width: 1000px;
  margin: 0 auto;
  padding: 24px;
  color: #e9edf5;
  font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
}

h1 {
  margin-bottom: 16px;
}

h2 {
  margin-top: 0;
}

.panel {
  background: #1f2432;
  border: 1px solid #2f3648;
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 16px;
}

.form-grid {
  display: grid;
  gap: 10px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-bottom: 10px;
}

.wide {
  grid-column: 1 / -1;
}

label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
}

input,
select,
textarea {
  border: 1px solid #4f5a77;
  background: #141928;
  color: #f6f8ff;
  border-radius: 8px;
  padding: 8px 10px;
}

.section-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.card {
  border: 1px solid #35425f;
  border-radius: 10px;
  padding: 12px;
  margin-top: 10px;
  background: #171d2b;
}

.desc {
  margin-bottom: 10px;
  color: #c9d1e8;
}

.actions {
  margin-bottom: 10px;
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
  justify-content: space-between;
  align-items: center;
  padding: 8px 10px;
  border: 1px solid #2f3a52;
  border-radius: 8px;
  margin-bottom: 6px;
}

.status {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
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
}

.inline-form input {
  flex: 1;
}

.primary,
.ghost {
  border-radius: 8px;
  padding: 8px 12px;
  border: 1px solid transparent;
  cursor: pointer;
}

.primary {
  background: #2563eb;
  color: white;
}

.ghost {
  background: transparent;
  color: #dbe7ff;
  border-color: #415276;
}

.error-box,
.success-box,
.empty {
  border-radius: 8px;
  padding: 10px 12px;
  margin: 10px 0;
}

.error-box {
  background: #4c1d1d;
  border: 1px solid #7f1d1d;
}

.success-box {
  background: #113124;
  border: 1px solid #166534;
}

.empty {
  background: #172036;
  border: 1px solid #24314f;
}

@media (max-width: 700px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .inline-form {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>