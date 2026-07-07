<script setup>
import { computed, ref, onMounted, onUnmounted, watch } from "vue";
import { login } from "./api/auth";
import { setAccessToken } from "./api/http";
import { completeAndRemoveStudyGoal, createStudyGoal, getCompletedStudyGoals, getStudyGoals } from "./api/goals";
import { createMilestone, getMilestones, trackMilestoneStudyTime, updateMilestoneStatus } from "./api/milestones";

const tokenStorageKey = "noter.jwt";
const dismissedRemindersStoragePrefix = "noter.dismissedReminders";
const goalReminderWindowMs = 7 * 24 * 60 * 60 * 1000;
const milestoneReminderWindowMs = 2 * 24 * 60 * 60 * 1000;

const goals = ref([]);
const errorMsg = ref("");
const successMsg = ref("");
const infoMsg = ref("");
const isLoadingGoals = ref(false);
const isCreatingGoal = ref(false);
const isCreatingMilestoneByGoal = ref({});
const isUpdatingMilestoneById = ref({});
const isCompletingGoalById = ref({});
const isTrackingTimer = ref(false);
const isAuthenticating = ref(false);
const isLoadingReminderMilestones = ref(false);
const authToken = ref(localStorage.getItem(tokenStorageKey) || "");
const currentUserId = ref("");
const currentUserEmail = ref("");
const completedGoalsArchive = ref([]);
const dismissedReminderIds = ref([]);
const isNotificationPanelOpen = ref(false);
const authForm = ref({
  email: "",
  password: ""
});
const isAuthenticated = computed(() => Boolean(authToken.value));

// Timer-State
const timerSeconds = ref(0);
const timerRunning = ref(false);
const timerStopped = ref(false);
const selectedMilestoneId = ref("");
let timerInterval = null;
let errorBannerTimeout = null;
let successBannerTimeout = null;
let infoBannerTimeout = null;
const bannerDurationMs = 3800;

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
const isLoadingMilestonesByGoal = ref({});
const relativeTimeFormatter = new Intl.RelativeTimeFormat("de-DE", {
  numeric: "auto"
});

setAccessToken(authToken.value);

watch(authToken, (value) => {
  setAccessToken(value);
  if (!value) {
    currentUserId.value = "";
    currentUserEmail.value = "";
    goalForm.value.userId = "";
    goals.value = [];
    milestonesByGoal.value = {};
    openGoals.value = {};
    selectedMilestoneId.value = "";
    completedGoalsArchive.value = [];
    dismissedReminderIds.value = [];
    isNotificationPanelOpen.value = false;
  }
});

if (authToken.value) {
  currentUserId.value = decodeUserId(authToken.value);
  currentUserEmail.value = decodeUserEmail(authToken.value);
  if (currentUserId.value) {
    goalForm.value.userId = currentUserId.value;
  }
}

loadReminderState();

watch(currentUserId, () => {
  loadReminderState();
});

function getScopedStorageKey(prefix) {
  return `${prefix}.${currentUserId.value || "guest"}`;
}

function readStoredJson(key, fallback) {
  try {
    const rawValue = localStorage.getItem(key);
    if (!rawValue) {
      return fallback;
    }

    return JSON.parse(rawValue);
  } catch {
    return fallback;
  }
}

function loadReminderState() {
  dismissedReminderIds.value = readStoredJson(getScopedStorageKey(dismissedRemindersStoragePrefix), []);
}

function persistDismissedReminderIds() {
  localStorage.setItem(
    getScopedStorageKey(dismissedRemindersStoragePrefix),
    JSON.stringify(dismissedReminderIds.value)
  );
}

function getApiErrorMessage(err) {
  const status = err.response?.status;
  const detail = err.response?.data?.detail;
  const title = err.response?.data?.title;
  const message = err.response?.data?.message;
  const errors = err.response?.data?.errors;

  if (status === 401) {
    return "Sitzung abgelaufen oder nicht mehr gueltig. Bitte erneut anmelden.";
  }

  if (detail) {
    return detail;
  }

  if (title) {
    return title;
  }

  if (message) {
    return message;
  }

  if (Array.isArray(errors) && errors.length > 0) {
    return errors.join(", ");
  }

  return status || err.message;
}

function scheduleBannerReset(kind) {
  if (kind === "error") {
    if (errorBannerTimeout) {
      clearTimeout(errorBannerTimeout);
    }

    errorBannerTimeout = setTimeout(() => {
      errorMsg.value = "";
    }, bannerDurationMs);
    return;
  }

  if (kind === "success") {
    if (successBannerTimeout) {
      clearTimeout(successBannerTimeout);
    }

    successBannerTimeout = setTimeout(() => {
      successMsg.value = "";
    }, bannerDurationMs);
    return;
  }

  if (infoBannerTimeout) {
    clearTimeout(infoBannerTimeout);
  }

  infoBannerTimeout = setTimeout(() => {
    infoMsg.value = "";
  }, bannerDurationMs);
}

watch(errorMsg, (value) => {
  if (value) {
    scheduleBannerReset("error");
  }
});

watch(successMsg, (value) => {
  if (value) {
    scheduleBannerReset("success");
  }
});

watch(infoMsg, (value) => {
  if (value) {
    scheduleBannerReset("info");
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
    const [activeGoals, completedGoals] = await Promise.all([
      getStudyGoals(),
      getCompletedStudyGoals()
    ]);

    goals.value = activeGoals;
    completedGoalsArchive.value = completedGoals;
    if (!goalForm.value.userId && currentUserId.value) {
      goalForm.value.userId = currentUserId.value;
    }

    await ensureReminderMilestonesLoaded(goals.value.map((goal) => goal.id));
  } catch (err) {
    errorMsg.value = "Lernziele konnten nicht geladen werden: " + (err.response?.status || err.message);
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

function translateAuthErrorMessage(message) {
  if (!message) {
    return "";
  }

  const normalized = String(message).trim().toLowerCase();

  if (
    normalized === "wrong password"
    || normalized === "password does not found"
    || normalized === "invalid credentials"
    || normalized === "falsche anmeldedaten."
  ) {
    return "Falsche Anmeldedaten.";
  }

  return message;
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
    currentUserEmail.value = decodeUserEmail(result.token) || authForm.value.email.trim();
    if (currentUserId.value) {
      goalForm.value.userId = currentUserId.value;
    }
    successMsg.value = `Angemeldet als ${authForm.value.email.trim()}.`;
    await loadGoals();
  } catch (err) {
    const apiErrors = Array.isArray(err.response?.data?.errors)
      ? err.response.data.errors.map((message) => translateAuthErrorMessage(message)).join(", ")
      : "";
    const fallbackMessage = translateAuthErrorMessage(err.message);
    errorMsg.value = "Anmeldung fehlgeschlagen: " + (apiErrors || fallbackMessage);
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
    return (
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]
      || payload.nameid
      || payload.Id
      || payload.sub
      || ""
    );
  } catch {
    return "";
  }
}

function decodeUserEmail(token) {
  try {
    const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const payload = JSON.parse(atob(padded));
    return payload.email || payload.sub || "";
  } catch {
    return "";
  }
}

async function fetchMilestonesForGoal(goalId, options = {}) {
  const { force = false, silent = false } = options;

  if (!force && Array.isArray(milestonesByGoal.value[goalId])) {
    return milestonesByGoal.value[goalId];
  }

  if (isLoadingMilestonesByGoal.value[goalId]) {
    return milestonesByGoal.value[goalId] || null;
  }

  isLoadingMilestonesByGoal.value = {
    ...isLoadingMilestonesByGoal.value,
    [goalId]: true
  };

  try {
    const data = await getMilestones(goalId);
    milestonesByGoal.value[goalId] = data;
    return data;
  } catch (err) {
    if (!silent) {
      errorMsg.value = "Meilensteine konnten nicht geladen werden: " + getApiErrorMessage(err);
    }

    return null;
  } finally {
    isLoadingMilestonesByGoal.value = {
      ...isLoadingMilestonesByGoal.value,
      [goalId]: false
    };
  }
}

async function ensureReminderMilestonesLoaded(goalIds = []) {
  if (!isAuthenticated.value || goalIds.length === 0) {
    return;
  }

  const missingGoalIds = goalIds.filter((goalId) => (
    !Array.isArray(milestonesByGoal.value[goalId])
    && !isLoadingMilestonesByGoal.value[goalId]
  ));

  if (missingGoalIds.length === 0) {
    return;
  }

  isLoadingReminderMilestones.value = true;

  try {
    await Promise.all(
      missingGoalIds.map((goalId) => fetchMilestonesForGoal(goalId, { silent: true }))
    );
  } finally {
    isLoadingReminderMilestones.value = false;
  }
}

// Timer-Funktionen
const formatTimer = (seconds) => {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = seconds % 60;
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
};

const formatMinutes = (minutes) => {
  if (!minutes || minutes <= 0) {
    return "0 Min";
  }

  const hours = Math.floor(minutes / 60);
  const restMinutes = minutes % 60;

  if (hours === 0) {
    return `${restMinutes} Min`;
  }

  if (restMinutes === 0) {
    return `${hours} Std`;
  }

  return `${hours} Std ${restMinutes} Min`;
};

const timerMilestoneOptions = computed(() => {
  const goalTitleById = goals.value.reduce((acc, goal) => {
    acc[goal.id] = goal.title;
    return acc;
  }, {});

  return Object.entries(milestonesByGoal.value)
    .filter(([goalId]) => Boolean(openGoals.value[goalId]))
    .flatMap(([goalId, milestones]) => (milestones || []).map((milestone) => ({
      id: milestone.id,
      goalId,
      goalTitle: goalTitleById[goalId] || "Lernziel",
      milestoneTitle: milestone.title
    })))
    .sort((a, b) => `${a.goalTitle} ${a.milestoneTitle}`.localeCompare(`${b.goalTitle} ${b.milestoneTitle}`, "de"));
});

const completedGoalsSummary = computed(() => {
  return completedGoalsArchive.value.reduce((summary, goal) => {
    summary.goalCount += 1;
    summary.totalTrackedMinutes += goal.totalTrackedMinutes || 0;
    summary.completedMilestones += goal.completedMilestones || 0;
    summary.totalMilestones += goal.totalMilestones || 0;
    return summary;
  }, {
    goalCount: 0,
    totalTrackedMinutes: 0,
    completedMilestones: 0,
    totalMilestones: 0
  });
});

const reminderItems = computed(() => {
  const now = Date.now();
  const goalTitleById = goals.value.reduce((acc, goal) => {
    acc[goal.id] = goal.title;
    return acc;
  }, {});
  const reminders = [];

  for (const goal of goals.value) {
    const endTimestamp = new Date(goal.endDate).getTime();
    const diffMs = endTimestamp - now;

    if (Number(goal.status) !== 2 && diffMs >= 0 && diffMs <= goalReminderWindowMs) {
      reminders.push({
        id: `goal:${goal.id}:${goal.endDate}`,
        type: "goal",
        title: `Lernziel endet bald: ${goal.title}`,
        description: `Dieses Lernziel endet ${formatRelativeTime(diffMs)} am ${formatDate(goal.endDate)}.`,
        dueAt: goal.endDate
      });
    }
  }

  for (const [goalId, milestones] of Object.entries(milestonesByGoal.value)) {
    for (const milestone of milestones || []) {
      const endTimestamp = new Date(milestone.endDateTime).getTime();
      const diffMs = endTimestamp - now;

      if (Number(milestone.status) !== 2 && diffMs >= 0 && diffMs <= milestoneReminderWindowMs) {
        reminders.push({
          id: `milestone:${milestone.id}:${milestone.endDateTime}`,
          type: "milestone",
          title: `Meilenstein faellig: ${milestone.title}`,
          description: `${goalTitleById[goalId] || "Lernziel"} endet ${formatRelativeTime(diffMs)} am ${formatDateTime(milestone.endDateTime)}.`,
          dueAt: milestone.endDateTime
        });
      }
    }
  }

  return reminders
    .filter((item) => !dismissedReminderIds.value.includes(item.id))
    .sort((a, b) => new Date(a.dueAt).getTime() - new Date(b.dueAt).getTime());
});

const formatTimerMilestoneOptionLabel = (option) => {
  const label = `${option.goalTitle} - ${option.milestoneTitle}`.replace(/\s+/g, " ").trim();
  const maxChars = 58;

  if (label.length <= maxChars) {
    return label;
  }

  return `${label.slice(0, maxChars - 3)}...`;
};

const formatDate = (value) => {
  if (!value) return "Kein Datum gesetzt";
  const date = new Date(value);
  return new Intl.DateTimeFormat("de-DE", {
    dateStyle: "medium"
  }).format(date);
};

const formatRelativeTime = (diffMs) => {
  const minutes = Math.max(1, Math.round(diffMs / (60 * 1000)));

  if (minutes < 60) {
    return relativeTimeFormatter.format(minutes, "minute");
  }

  const hours = Math.round(diffMs / (60 * 60 * 1000));
  if (hours < 48) {
    return relativeTimeFormatter.format(hours, "hour");
  }

  const days = Math.round(diffMs / (24 * 60 * 60 * 1000));
  return relativeTimeFormatter.format(days, "day");
};

const goalAchievementLabel = (goal) => {
  if (!goal.totalMilestones) {
    return "Ziel abgeschlossen";
  }

  return `${goal.completedMilestones} von ${goal.totalMilestones} Teilzielen erreicht`;
};

const goalAchievementRate = (goal) => {
  if (!goal.totalMilestones) {
    return 100;
  }

  return Math.round((goal.completedMilestones / goal.totalMilestones) * 100);
};

function dismissReminder(reminderId) {
  if (dismissedReminderIds.value.includes(reminderId)) {
    return;
  }

  dismissedReminderIds.value = [...dismissedReminderIds.value, reminderId];
  persistDismissedReminderIds();
}

function dismissAllReminders() {
  const reminderIds = reminderItems.value.map((item) => item.id);
  if (reminderIds.length === 0) {
    return;
  }

  dismissedReminderIds.value = [...new Set([...dismissedReminderIds.value, ...reminderIds])];
  persistDismissedReminderIds();
}

async function toggleNotificationPanel() {
  isNotificationPanelOpen.value = !isNotificationPanelOpen.value;

  if (isNotificationPanelOpen.value) {
    await ensureReminderMilestonesLoaded(goals.value.map((goal) => goal.id));
  }
}

const findGoalIdByMilestoneId = (milestoneId) => {
  for (const [goalId, milestones] of Object.entries(milestonesByGoal.value)) {
    if ((milestones || []).some((milestone) => milestone.id === milestoneId)) {
      return goalId;
    }
  }

  return null;
};

watch(timerMilestoneOptions, (options) => {
  if (!selectedMilestoneId.value) {
    return;
  }

  const hasSelection = options.some((option) => option.id === selectedMilestoneId.value);
  if (!hasSelection) {
    selectedMilestoneId.value = "";
  }
});

const startTimer = () => {
  errorMsg.value = "";
  successMsg.value = "";

  if (!selectedMilestoneId.value) {
    infoMsg.value = "Bitte zuerst die Meilensteine bei einem Lernziel anzeigen lassen und anschliessend einen fuer die Stoppuhr auswaehlen.";
    return;
  }

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

const stopTimer = async () => {
  errorMsg.value = "";
  successMsg.value = "";

  timerRunning.value = false;
  timerStopped.value = true;
  clearInterval(timerInterval);

  if (!selectedMilestoneId.value || timerSeconds.value <= 0) {
    return;
  }

  const trackedMinutes = Math.max(1, Math.ceil(timerSeconds.value / 60));
  isTrackingTimer.value = true;

  try {
    await trackMilestoneStudyTime(selectedMilestoneId.value, trackedMinutes);

    const goalId = findGoalIdByMilestoneId(selectedMilestoneId.value);
    if (goalId) {
      await fetchMilestonesForGoal(goalId, { force: true, silent: true });
    }

    await loadGoals();
    successMsg.value = `Lernzeit gespeichert: ${formatMinutes(trackedMinutes)}.`;
  } catch (err) {
    errorMsg.value = "Lernzeit konnte nicht gespeichert werden: " + getApiErrorMessage(err);
  } finally {
    isTrackingTimer.value = false;
  }
};

const resetTimer = () => {
  timerRunning.value = false;
  timerStopped.value = false;
  timerSeconds.value = 0;
  clearInterval(timerInterval);
};

const showTimerMilestoneHint = () => {
  if (timerMilestoneOptions.value.length > 0) {
    return;
  }

  infoMsg.value = "Bitte zuerst die Meilensteine bei einem Lernziel anzeigen lassen und anschliessend einen fuer die Stoppuhr auswaehlen.";
};

onMounted(loadGoals);

onUnmounted(() => {
  if (timerInterval) {
    clearInterval(timerInterval);
  }

  if (errorBannerTimeout) {
    clearTimeout(errorBannerTimeout);
  }

  if (successBannerTimeout) {
    clearTimeout(successBannerTimeout);
  }

  if (infoBannerTimeout) {
    clearTimeout(infoBannerTimeout);
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

  if (Array.isArray(milestonesByGoal.value[goalId])) {
    return;
  }

  await fetchMilestonesForGoal(goalId);
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

    successMsg.value = "Lernziel wurde erstellt.";
    goalForm.value.title = "";
    goalForm.value.description = "";
    await loadGoals();
  } catch (err) {
    errorMsg.value = "Lernziel konnte nicht erstellt werden: " + (err.response?.status || err.message);
  } finally {
    isCreatingGoal.value = false;
  }
};

const completeAndRemoveGoal = async (goalId) => {
  errorMsg.value = "";
  successMsg.value = "";

  if (!ensureAuthenticated("Das Abschliessen von Lernzielen")) {
    return;
  }

  if (!Array.isArray(milestonesByGoal.value[goalId])) {
    const loadedMilestones = await fetchMilestonesForGoal(goalId);
    if (!loadedMilestones) {
      return;
    }
  }

  const milestones = milestonesByGoal.value[goalId] || [];
  const hasOpenMilestones = milestones.some((milestone) => Number(milestone.status) !== 2);

  if (hasOpenMilestones) {
    errorMsg.value = "Vor dem Abschliessen muessen alle Meilensteine auf 'Abgeschlossen' stehen.";
    return;
  }

  isCompletingGoalById.value = {
    ...isCompletingGoalById.value,
    [goalId]: true
  };

  try {
    await completeAndRemoveStudyGoal(goalId);

    delete milestonesByGoal.value[goalId];
    delete openGoals.value[goalId];
    await loadGoals();

    successMsg.value = "Lernziel wurde abgeschlossen.";
  } catch (err) {
    errorMsg.value = "Lernziel konnte nicht abgeschlossen werden: " + getApiErrorMessage(err);
  } finally {
    isCompletingGoalById.value = {
      ...isCompletingGoalById.value,
      [goalId]: false
    };
  }
};

const ensureMilestoneForm = (goalId) => {
  if (!milestoneFormByGoal.value[goalId]) {
    const now = new Date();
    const plusHour = new Date(now.getTime() + 60 * 60 * 1000);
    milestoneFormByGoal.value[goalId] = {
      title: "",
      startDate: now.toISOString().slice(0, 10),
      startTime: now.toTimeString().slice(0, 5),
      endDate: plusHour.toISOString().slice(0, 10),
      endTime: plusHour.toTimeString().slice(0, 5)
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

  const startDate = milestoneFormByGoal.value[goalId].startDate;
  const startTime = milestoneFormByGoal.value[goalId].startTime;
  const endDate = milestoneFormByGoal.value[goalId].endDate;
  const endTime = milestoneFormByGoal.value[goalId].endTime;

  if (!startDate || !startTime || !endDate || !endTime) {
    errorMsg.value = "Bitte Start- und Endzeit fuer den Meilenstein angeben.";
    return;
  }

  const startDateTime = new Date(`${startDate}T${startTime}:00`);
  const endDateTime = new Date(`${endDate}T${endTime}:00`);

  if (startDateTime >= endDateTime) {
    errorMsg.value = "Die Endzeit muss nach der Startzeit liegen.";
    return;
  }

  isCreatingMilestoneByGoal.value = {
    ...isCreatingMilestoneByGoal.value,
    [goalId]: true
  };

  try {
    await createMilestone({
      studyGoalId: goalId,
      title,
      startDateTime: startDateTime.toISOString(),
      endDateTime: endDateTime.toISOString()
    });

    milestoneFormByGoal.value[goalId].title = "";
    await fetchMilestonesForGoal(goalId, { force: true, silent: true });
    successMsg.value = "Meilenstein wurde erstellt.";
  } catch (err) {
    errorMsg.value = "Meilenstein konnte nicht erstellt werden: " + getApiErrorMessage(err);
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
    errorMsg.value = "Status konnte nicht aktualisiert werden: " + getApiErrorMessage(err);
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

const formatDateTime = (value) => {
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
    <div class="banner-stack" aria-live="polite">
      <p v-if="successMsg" class="banner banner-success">{{ successMsg }}</p>
      <p v-if="errorMsg" class="banner banner-error">{{ errorMsg }}</p>
      <p v-if="infoMsg" class="banner banner-info">{{ infoMsg }}</p>
    </div>

    <header v-if="isAuthenticated" class="hero panel">
      <div class="hero-topbar">
        <div class="hero-copy">
          <p class="eyebrow">Lernzeit-Manager</p>
          <h1>Lernziele strukturiert planen</h1>
          <p class="lead">
            Erstelle Lernziele, zerlege sie in Meilensteine und behalte deinen Fortschritt auf jedem Gerät im Blick.
          </p>
        </div>

        <div class="notification-shell">
          <button
            type="button"
            class="ghost notification-bell"
            :aria-expanded="isNotificationPanelOpen"
            @click="toggleNotificationPanel"
          >
            <span class="notification-bell-icon">&#128276;</span>
            <span>Erinnerungen</span>
            <span v-if="reminderItems.length > 0" class="notification-count">{{ reminderItems.length }}</span>
          </button>

          <div v-if="isNotificationPanelOpen" class="notification-popover">
            <div class="notification-head">
              <div>
                <h2>Erinnerungen</h2>
                <p class="panel-note">Lernziele 7 Tage vorher, Meilensteine 2 Tage vorher.</p>
              </div>
              <button
                v-if="reminderItems.length > 0"
                type="button"
                class="ghost notification-clear"
                @click="dismissAllReminders"
              >
                Alle entfernen
              </button>
            </div>

            <p v-if="isLoadingReminderMilestones" class="panel-note">
              Erinnerungen werden aktualisiert...
            </p>

            <div v-if="reminderItems.length > 0" class="notification-list">
              <article v-for="item in reminderItems" :key="item.id" class="notification-item">
                <div class="notification-item-head">
                  <div>
                    <strong>{{ item.title }}</strong>
                    <p class="panel-note notification-copy">{{ item.description }}</p>
                  </div>
                  <span class="badge" :class="item.type === 'goal' ? 'badge-goal' : 'badge-milestone'">
                    {{ item.type === "goal" ? "Lernziel" : "Meilenstein" }}
                  </span>
                </div>

                <div class="notification-actions">
                  <button type="button" class="ghost" @click="dismissReminder(item.id)">
                    Entfernen
                  </button>
                </div>
              </article>
            </div>

            <p v-else class="empty notification-empty">
              Aktuell gibt es keine offenen Erinnerungen.
            </p>
          </div>
        </div>
      </div>
    </header>

    <section v-if="!isAuthenticated" class="panel auth-panel">
      <div class="section-head">
        <div>
          <h1>Lernzeit-Manager</h1>
          <p class="panel-note">Melde dich an, um deine Lernziele und Meilensteine zu verwalten.</p>
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
          {{ isAuthenticating ? "Anmeldung..." : "Anmelden" }}
        </button>
        <button class="ghost" :disabled="!isAuthenticated" @click="logout">
          Abmelden
        </button>
      </div>

    </section>

    <section v-else class="panel auth-panel">
      <div class="section-head">
        <div>
          <h2>Angemeldet</h2>
          <p class="panel-note">Aktuell eingeloggt als: {{ currentUserEmail || authForm.email }}</p>
        </div>
        <button class="ghost" @click="logout">Abmelden</button>
      </div>
    </section>

    <section v-if="isAuthenticated" class="panel timer-panel">
      <div class="timer-header">
        <h2>Lernzeit-Stoppuhr</h2>
      </div>

      <div class="timer-target">
        <label for="timer-milestone">Meilenstein fuer diese Session</label>
        <select
          id="timer-milestone"
          v-model="selectedMilestoneId"
          :disabled="timerRunning || isTrackingTimer"
          @focus="showTimerMilestoneHint"
        >
          <option value="">Bitte waehlen...</option>
          <option
            v-for="option in timerMilestoneOptions"
            :key="option.id"
            :value="option.id"
          >
            {{ formatTimerMilestoneOptionLabel(option) }}
          </option>
        </select>
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
          :disabled="timerStopped || timerSeconds === 0 || isTrackingTimer"
          @click="stopTimer"
        >
          {{ isTrackingTimer ? "Speichere..." : "⏹ Stop" }}
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
        <h2>Neues Lernziel erstellen</h2>
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
      </div>

      <button class="primary" :disabled="isCreatingGoal" @click="submitGoal">
        {{ isCreatingGoal ? "Speichern..." : "Lernziel erstellen" }}
      </button>
    </section>

    <section v-if="isAuthenticated" class="panel">
      <div class="section-head">
        <h2>Vorhandene Lernziele</h2>
        <button class="ghost" :disabled="isLoadingGoals" @click="loadGoals">
          {{ isLoadingGoals ? "Lade..." : "Neu laden" }}
        </button>
      </div>

      <p v-if="!isLoadingGoals && goals.length === 0" class="empty">
        Keine Ziele vorhanden. Erstelle oben dein erstes Lernziel.
      </p>

      <article v-for="g in goals" :key="g.id" class="card">
        <div class="card-header">
          <h3>{{ g.title }}</h3>
          <span class="badge type">{{ typeLabel(g.type) }}</span>
        </div>
        <p class="desc">{{ g.description }}</p>
        <div class="goal-date-grid">
          <span class="date-chip"><strong>Start:</strong> {{ formatDate(g.startDate) }}</span>
          <span class="date-chip"><strong>Ende:</strong> {{ formatDate(g.endDate) }}</span>
        </div>
        <p class="panel-note"><strong>Gesamte Lernzeit:</strong> {{ formatMinutes(g.totalTrackedMinutes) }}</p>

        <div class="actions">
          <button class="ghost" @click="toggleMilestones(g.id)">
            {{ openGoals[g.id] ? "Meilensteine verbergen" : "Meilensteine anzeigen" }}
          </button>
          <button
            class="primary"
            :disabled="isCompletingGoalById[g.id]"
            @click="completeAndRemoveGoal(g.id)"
          >
            {{ isCompletingGoalById[g.id] ? "Pruefe..." : "Abschliessen und entfernen" }}
          </button>
        </div>

        <div v-if="openGoals[g.id]" class="milestone-wrap">
          <ul v-if="milestonesByGoal[g.id]" class="milestone-list">
            <li v-for="m in milestonesByGoal[g.id]" :key="m.id" class="milestone">
              <div class="milestone-info">
                <button
                  type="button"
                  class="status status-large"
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
                <span class="title">{{ m.title }}</span>
                <span class="panel-note">Start: {{ formatDateTime(m.startDateTime) }}</span>
                <span class="panel-note">Ende: {{ formatDateTime(m.endDateTime) }}</span>
                <span class="panel-note">
                  Sessions: {{ m.sessionCount || 0 }} | Lernzeit: {{ formatMinutes(m.totalTrackedMinutes || 0) }}
                </span>
              </div>
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
              :value="milestoneFormByGoal[g.id]?.startDate || ''"
              type="date"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].startDate = $event.target.value"
            />
            <input
              :value="milestoneFormByGoal[g.id]?.startTime || ''"
              type="time"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].startTime = $event.target.value"
            />
            <input
              :value="milestoneFormByGoal[g.id]?.endDate || ''"
              type="date"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].endDate = $event.target.value"
            />
            <input
              :value="milestoneFormByGoal[g.id]?.endTime || ''"
              type="time"
              @input="ensureMilestoneForm(g.id); milestoneFormByGoal[g.id].endTime = $event.target.value"
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

    <section v-if="isAuthenticated" class="panel">
      <div class="section-head">
        <div>
          <h2>Abgeschlossene Lernziele</h2>
          <p class="panel-note">Hier hast du einen Überblick und eine Zusammenfassung deiner Lernziele und deiner Lernzeit.</p>
        </div>
        <span class="badge badge-completed">{{ completedGoalsSummary.goalCount }} abgeschlossen</span>
      </div>

      <div v-if="completedGoalsArchive.length > 0" class="completed-summary-grid">
        <article class="summary-card">
          <span class="summary-label">Abgeschlossene Ziele</span>
          <strong>{{ completedGoalsSummary.goalCount }}</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Gesamte Lernzeit</span>
          <strong>{{ formatMinutes(completedGoalsSummary.totalTrackedMinutes) }}</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Erreichte Teilziele</span>
          <strong>{{ completedGoalsSummary.completedMilestones }} / {{ completedGoalsSummary.totalMilestones }}</strong>
        </article>
      </div>

      <p v-if="completedGoalsArchive.length === 0" class="empty">
        Noch keine abgeschlossenen Lernziele archiviert.
      </p>

      <div v-else class="archive-list">
        <article v-for="goal in completedGoalsArchive" :key="goal.id" class="card completed-card">
          <div class="card-header">
            <h3>{{ goal.title }}</h3>
            <span class="badge badge-completed">Abgeschlossen</span>
          </div>

          <p class="desc">{{ goal.description }}</p>

          <div class="goal-date-grid">
            <span class="date-chip"><strong>Start:</strong> {{ formatDate(goal.startDate) }}</span>
            <span class="date-chip"><strong>Ende:</strong> {{ formatDate(goal.endDate) }}</span>
            <span class="date-chip"><strong>Abgeschlossen:</strong> {{ formatDate(goal.endDate) }}</span>
          </div>

          <div class="completed-metrics-grid">
            <article class="summary-card">
              <span class="summary-label">Lernzeit</span>
              <strong>{{ formatMinutes(goal.totalTrackedMinutes) }}</strong>
            </article>
            <article class="summary-card">
              <span class="summary-label">Zielerreichung</span>
              <strong>{{ goalAchievementLabel(goal) }}</strong>
            </article>
          </div>

          <div class="achievement-track" aria-hidden="true">
            <span class="achievement-bar" :style="{ width: `${goalAchievementRate(goal)}%` }"></span>
          </div>
          <p class="panel-note">Erreichungsquote: {{ goalAchievementRate(goal) }}%</p>
        </article>
      </div>
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

.banner-stack {
  position: fixed;
  top: clamp(12px, 2.5vw, 20px);
  left: 50%;
  transform: translateX(-50%);
  z-index: 1200;
  width: min(760px, calc(100% - 24px));
  display: grid;
  gap: 8px;
  pointer-events: none;
}

.banner {
  border-radius: 12px;
  padding: 10px 14px;
  margin: 0;
  box-shadow: 0 12px 30px rgba(3, 8, 18, 0.45);
  backdrop-filter: blur(4px);
  border: 1px solid transparent;
  color: #eef3ff;
}

.banner-success {
  background: rgba(14, 72, 56, 0.92);
  border-color: #1d7a59;
}

.banner-error {
  background: rgba(80, 31, 39, 0.94);
  border-color: #9f3346;
}

.banner-info {
  background: rgba(22, 43, 77, 0.94);
  border-color: #416aac;
}

.hero {
  padding: clamp(18px, 3vw, 28px);
  background:
    radial-gradient(circle at 85% 15%, rgba(106, 196, 255, 0.25), transparent 30%),
    radial-gradient(circle at 15% 10%, rgba(52, 211, 153, 0.2), transparent 32%),
    linear-gradient(140deg, #141a2b 0%, #101827 100%);
}

.hero-topbar {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
}

.hero-copy {
  flex: 1;
  min-width: 0;
}

.notification-shell {
  position: relative;
  width: min(360px, 100%);
}

.notification-bell {
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.notification-bell-icon {
  font-size: 1.05rem;
}

.notification-count {
  min-width: 28px;
  height: 28px;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: #ef4444;
  color: #fff;
  font-weight: 700;
  padding: 0 8px;
}

.notification-popover {
  position: absolute;
  top: calc(100% + 10px);
  right: 0;
  z-index: 20;
  width: min(360px, calc(100vw - 48px));
  padding: 14px;
  border-radius: 16px;
  border: 1px solid #34456b;
  background: rgba(13, 21, 40, 0.98);
  box-shadow: 0 20px 44px rgba(3, 8, 18, 0.42);
  display: grid;
  gap: 12px;
}

.notification-head,
.notification-item-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 10px;
}

.notification-clear {
  width: auto;
}

.notification-list,
.archive-list {
  display: grid;
  gap: 12px;
}

.notification-item {
  display: grid;
  gap: 10px;
  border-radius: 12px;
  padding: 12px;
  background: rgba(19, 30, 52, 0.95);
  border: 1px solid #33466f;
}

.notification-copy {
  margin-top: 4px;
}

.notification-actions {
  display: flex;
  justify-content: flex-end;
}

.notification-empty {
  margin: 0;
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
  min-width: 0;
}

.timer-panel {
  text-align: center;
}

.timer-header {
  margin-bottom: 14px;
}

.timer-target {
  max-width: 680px;
  width: 100%;
  min-width: 0;
  margin: 0 auto 14px;
  display: grid;
  gap: 6px;
  text-align: left;
}

.timer-target label {
  font-weight: 700;
}

.timer-target select {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
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
  width: 100%;
  max-width: 100%;
  min-width: 0;
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
  width: 100%;
  min-width: 0;
  overflow: hidden;
}

.desc {
  margin: 6px 0 12px;
  color: #c9d1e8;
}

.goal-date-grid,
.completed-summary-grid,
.completed-metrics-grid {
  display: grid;
  gap: 10px;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
}

.goal-date-grid {
  margin-bottom: 12px;
}

.date-chip,
.summary-card {
  display: grid;
  gap: 4px;
  padding: 10px 12px;
  border-radius: 12px;
  border: 1px solid #31466f;
  background: rgba(11, 18, 33, 0.65);
  min-width: 0;
}

.summary-label {
  color: #9fb6e8;
  font-size: 0.86rem;
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
  min-width: 0;
  overflow-wrap: anywhere;
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

.badge-goal {
  background: rgba(59, 130, 246, 0.18);
  color: #b8d8ff;
  border: 1px solid #4e82d4;
}

.badge-milestone {
  background: rgba(245, 158, 11, 0.18);
  color: #f7d28c;
  border: 1px solid #d59b21;
}

.badge-completed {
  background: rgba(16, 185, 129, 0.18);
  color: #86efc2;
  border: 1px solid #1f9d73;
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

.completed-card {
  border-color: #2b6b5a;
}

.achievement-track {
  margin-top: 12px;
  width: 100%;
  height: 10px;
  border-radius: 999px;
  background: rgba(72, 94, 135, 0.32);
  overflow: hidden;
}

.achievement-bar {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #34d399, #10b981);
}

.actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 12px;
}

.milestone-wrap {
  margin-top: 8px;
  min-width: 0;
  width: 100%;
  overflow-x: hidden;
}

.milestone-list {
  list-style: none;
  padding: 0;
  margin: 0 0 8px 0;
  min-width: 0;
}

.milestone {
  display: flex;
  gap: 10px;
  align-items: flex-start;
  padding: 10px 12px;
  border: 1px solid #2f3a52;
  border-radius: 10px;
  background: rgba(10, 16, 29, 0.45);
  margin-bottom: 6px;
  min-width: 0;
  width: 100%;
}

.milestone-info {
  display: grid;
  gap: 4px;
  width: 100%;
  min-width: 0;
}

.title {
  overflow-wrap: anywhere;
  word-break: break-word;
}

.panel-note {
  overflow-wrap: anywhere;
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

.status-large {
  width: fit-content;
  font-size: 15px;
  font-weight: 700;
  padding: 8px 16px;
  margin-bottom: 4px;
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
  display: grid;
  gap: 8px;
  align-items: end;
  grid-template-columns: repeat(auto-fit, minmax(min(180px, 100%), 1fr));
  margin-top: 8px;
  min-width: 0;
}

.inline-form input {
  min-width: 0;
  width: 100%;
}

.inline-form > * {
  min-width: 0;
  max-width: 100%;
}

.inline-form input[type="date"],
.inline-form input[type="time"] {
  min-width: 0;
}

.inline-form button {
  width: 100%;
  white-space: normal;
  overflow-wrap: anywhere;
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

.empty {
  border-radius: 10px;
  padding: 10px 12px;
  margin: 0;
}

.empty {
  background: #15223b;
  border: 1px solid #27487a;
}

@media (max-width: 900px) {
  .hero-topbar,
  .notification-head,
  .notification-item-head {
    flex-direction: column;
    align-items: stretch;
  }

  .notification-shell {
    width: 100%;
  }

  .notification-popover {
    position: static;
    width: 100%;
  }

  .section-head {
    flex-direction: column;
    align-items: flex-start;
  }

  .card {
    padding: 12px;
  }

  .card-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .badge {
    white-space: normal;
  }

  .status-large {
    width: 100%;
    text-align: left;
    white-space: normal;
  }

  .inline-form {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 700px) {
  .timer-time {
    font-size: clamp(2rem, 11vw, 3rem);
    letter-spacing: 0.08em;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }

  .auth-actions {
    flex-direction: column;
  }

  .goal-date-grid,
  .completed-summary-grid,
  .completed-metrics-grid {
    grid-template-columns: 1fr;
  }

  .milestone {
    flex-direction: column;
    align-items: flex-start;
  }

  .card,
  .milestone,
  .milestone-wrap,
  .milestone-info {
    width: 100%;
    min-width: 0;
  }

  .actions {
    flex-direction: column;
  }

  .inline-form {
    grid-template-columns: 1fr;
  }

  .milestone-info {
    width: 100%;
  }

  .primary,
  .ghost {
    width: 100%;
  }
}

@media (pointer: coarse) {
  input,
  select,
  textarea,
  button {
    font-size: 16px;
  }
}
</style>