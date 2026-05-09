<template>
  <section class="not-available">
    <h2>Page non disponible</h2>

    <p class="description">
      Cette page n’est pas accessible sur ce format d’écran.<br><br>
      Vous devriez aller chercher du travail afin de pouvoir vous acheter un écran plus grand, clodo.
    </p>

    <div class="compatibility">
      <!-- Mobile -->
      <div class="device blocked">
        <i class="mdi mdi-cellphone device-icon" />
        <i class="mdi mdi-close-circle status-icon error" />
      </div>

      <!-- Desktop -->
      <div class="device allowed">
        <i class="mdi mdi-monitor device-icon" />
        <i class="mdi mdi-check-circle status-icon success" />
      </div>
    </div>
  </section>
</template>

<style lang="scss" scoped>
.not-available {
  padding: 2rem 1rem;
  text-align: center;
  opacity: 0.9;

  h2 {
    color: var(--pico-primary);
    margin-bottom: 0.5rem;
  }

  .description {
    color: var(--pico-muted-color);
    margin-bottom: 2rem;
  }
}

.compatibility {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  align-items: center;

  @media (min-width: 600px) {
    flex-direction: row;
    justify-content: center;
    gap: 3rem;
  }
}

.device {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  min-width: 120px;

  .device-icon {
    font-size: 3rem;
    color: var(--pico-muted-color);
  }

  .status-icon {
    font-size: 1.5rem;
    margin-top: -0.75rem;
  }

  span {
    font-size: 0.9rem;
    color: var(--pico-muted-color);
  }
}

.status-icon.error {
  color: var(--pico-form-element-invalid-border-color);
}

.status-icon.success {
  color: var(--pico-form-element-valid-border-color);
}

.device.allowed .device-icon {
  // color: var(--pico-primary);
}

.device.blocked .device-icon {
  opacity: 0.6;
}
</style>
