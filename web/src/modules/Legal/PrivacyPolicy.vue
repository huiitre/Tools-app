```vue
<template>
  <main class="privacy-policy">
    <h1>Politique de confidentialité</h1>

    <section>
      <h2>1. Objet</h2>
      <p>
        La présente Politique de confidentialité a pour objectif d’informer les
        utilisateurs de l’application <strong>Tools</strong> sur la manière dont
        leurs données personnelles sont collectées, utilisées et protégées.
      </p>
    </section>

    <section>
      <h2>2. Données collectées</h2>
      <p>
        Dans le cadre de l’utilisation du Service, les données suivantes peuvent
        être collectées :
      </p>
      <ul>
        <li>adresse email,</li>
        <li>identifiants techniques nécessaires à l’authentification,</li>
        <li>données techniques (logs, adresses IP, informations de session),</li>
        <li>toute information fournie volontairement par l’utilisateur.</li>
      </ul>
    </section>

    <section>
      <h2>3. Finalité de la collecte</h2>
      <p>
        Les données personnelles sont collectées uniquement pour les finalités
        suivantes :
      </p>
      <ul>
        <li>fourniture et fonctionnement du Service,</li>
        <li>gestion des comptes utilisateurs,</li>
        <li>sécurisation des accès et prévention des abus,</li>
        <li>amélioration du Service et diagnostic technique.</li>
      </ul>
    </section>

    <section>
      <h2>4. Base légale du traitement</h2>
      <p>
        Les traitements de données reposent :
      </p>
      <ul>
        <li>sur l’exécution du Service demandé par l’utilisateur,</li>
        <li>sur l’intérêt légitime de l’éditeur à assurer la sécurité et la
          stabilité du Service,</li>
        <li>le cas échéant, sur le consentement explicite de l’utilisateur.</li>
      </ul>
    </section>

    <section>
      <h2>5. Conservation des données</h2>
      <p>
        Les données personnelles sont conservées uniquement pendant la durée
        nécessaire aux finalités pour lesquelles elles ont été collectées.
      </p>
      <p>
        Les comptes inactifs ou supprimés peuvent entraîner la suppression ou
        l’anonymisation des données associées, sauf obligation légale contraire.
      </p>
    </section>

    <section>
      <h2>6. Partage des données</h2>
      <p>
        Les données personnelles ne sont ni vendues ni cédées à des tiers.
      </p>
      <p>
        Elles peuvent toutefois être accessibles à des prestataires techniques
        strictement nécessaires au fonctionnement du Service (hébergement,
        infrastructure), dans le respect de la réglementation en vigueur.
      </p>
    </section>

    <section>
      <h2>7. Sécurité des données</h2>
      <p>
        L’éditeur met en œuvre des mesures techniques et organisationnelles
        raisonnables afin de protéger les données personnelles contre tout accès
        non autorisé, perte ou divulgation.
      </p>
      <p>
        Malgré ces mesures, aucun système informatique ne peut garantir une
        sécurité absolue.
      </p>
    </section>

    <section>
      <h2>8. Droits des utilisateurs</h2>
      <p>
        Conformément à la réglementation applicable, l’utilisateur dispose des
        droits suivants :
      </p>
      <ul>
        <li>droit d’accès à ses données,</li>
        <li>droit de rectification,</li>
        <li>droit de suppression,</li>
        <li>droit à la limitation du traitement,</li>
        <li>droit d’opposition,</li>
        <li>droit à la portabilité des données.</li>
      </ul>
      <p>
        Ces droits peuvent être exercés en contactant l’éditeur via les moyens
        prévus dans l’application.
      </p>
    </section>

    <section>
      <h2>9. Cookies et traceurs</h2>
      <p>
        Le Service peut utiliser des cookies ou mécanismes similaires strictement
        nécessaires à son fonctionnement (authentification, sécurité, session).
      </p>
      <p>
        Aucun cookie publicitaire ou de suivi à des fins commerciales n’est
        utilisé sans consentement explicite.
      </p>
    </section>

    <section>
      <h2>10. Modification de la politique</h2>
      <p>
        La présente Politique de confidentialité peut être modifiée à tout moment.
      </p>
      <p>
        Les utilisateurs seront informés de toute modification substantielle.
      </p>
    </section>

    <section>
      <h2>11. Droit applicable</h2>
      <p>
        La présente Politique de confidentialité est régie par le
        <strong>droit français</strong>.
      </p>
    </section>
  </main>
</template>

<script setup lang="ts">
</script>

<style scoped>
.privacy-policy {
  max-width: 800px;
  margin: 2rem auto;
  padding: 0 1rem;
}

section {
  margin-bottom: 1.5rem;
}

h1 {
  margin-bottom: 2rem;
  text-align: center;
}

h2 {
  margin-bottom: 0.5rem;
}

ul {
  padding-left: 1.25rem;
  list-style: disc;
}
</style>
```
