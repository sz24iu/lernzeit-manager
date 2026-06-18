<script setup>
import { ref, onMounted } from "vue";
import { getStudyGoals } from "./api/goals";
import { getMilestones } from "./api/milestones";

const goals = ref([]);

// goalId -> milestones
const milestonesByGoal = ref({});

// какие цели раскрыты
const openGoals = ref({});

onMounted(async () => {
  goals.value = await getStudyGoals();
});

const toggleMilestones = async (goalId) => {
  // открыть/закрыть
  openGoals.value[goalId] = !openGoals.value[goalId];

  // если уже загружено — не грузим повторно
  if (milestonesByGoal.value[goalId]) return;

  const data = await getMilestones(goalId);
  milestonesByGoal.value[goalId] = data;
};
</script>

<template>
  <div class="container">
    <h1>📚 Study Goals</h1>

    <div v-for="g in goals" :key="g.id" class="card">
      
      <!-- GOAL -->
      <h2>{{ g.title }}</h2>
      <p>{{ g.description }}</p>

      <!-- BUTTON -->
      <button @click="toggleMilestones(g.id)">
        {{ openGoals[g.id] ? "Hide" : "Show" }} Milestones
      </button>

      <!-- MILESTONES -->
      <div v-if="openGoals[g.id]" class="milestones">
        <ul v-if="milestonesByGoal[g.id]">
          <li
  v-for="m in milestonesByGoal[g.id]"
  :key="m.id"
  class="milestone"
>
  <span class="title">
    {{ m.title }}
  </span>

  <span
    class="status"
    :class="{
      planned: m.status === 0,
      progress: m.status === 1,
      done: m.status === 2,
      failed: m.status === 3
    }"
  >
    {{
      m.status === 0
        ? "Planned"
        : m.status === 1
        ? "In Progress"
        : m.status === 2
        ? "Completed"
        : "Failed"
    }}
  </span>
</li>
        </ul>

        <p v-else>Loading...</p>
      </div>

    </div>
  </div>
</template>

<style scoped>
.container {
  padding: 20px;
  font-family: Arial;
}

.card {
  background: #1e1e2f;
  color: white;
  padding: 15px;
  margin-bottom: 15px;
  border-radius: 10px;
}

button {
  margin-top: 10px;
  padding: 6px 10px;
  cursor: pointer;
}

.milestone {
  display: flex;
  justify-content: space-between;
  align-items: center;

  padding: 8px 12px;
  margin: 6px 0;

  background: #1f1f2e;
  border-radius: 8px;
  color: white;
}

.title {
  flex: 1;
}

/* общий стиль бейджа */
.status {
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: bold;
  min-width: 100px;
  text-align: center;
}

/* зелёный (done) */
.status.done {
  background: #22c55e;
  color: #0b1f0f;
}

/* жёлтый (in progress) */
.status.progress {
  background: #facc15;
  color: #1f1f1f;
}

.status.failed {
  background: red;
  color: #1f1f1f;
}

</style>