# Implémentation SignalR - Mise à jour en temps réel de l'historique

## Vue d'ensemble

Cette implémentation ajoute la mise à jour en temps réel de l'historique des besoins via SignalR (WebSocket). Lorsqu'une action est effectuée sur un besoin (création, modification, validation, signature, etc.), tous les utilisateurs consultant ce besoin reçoivent instantanément la mise à jour dans l'historique.

## Architecture

### Backend (ASP.NET Core)

#### 1. Hub SignalR
**Fichier:** `Backend/FinAssist.API/Hubs/BesoinsHub.cs`

Le hub gère les connexions WebSocket et les groupes par besoin:
- `JoinBesoinGroup(besoinId)` - Rejoindre le groupe d'un besoin spécifique
- `LeaveBesoinGroup(besoinId)` - Quitter le groupe d'un besoin

#### 2. Service Hub
**Fichier:** `Backend/FinAssist.Infrastructure/Services/BesoinsHubService.cs`

Service injectable qui envoie les notifications aux clients connectés:
- `NotifierHistoriqueAsync(besoinId, action, description)` - Notifie tous les clients du groupe

#### 3. Configuration
**Fichier:** `Backend/FinAssist.API/Program.cs`

- Ajout de `builder.Services.AddSignalR()`
- Configuration CORS avec `.AllowCredentials()` (requis pour SignalR)
- Mapping du hub: `app.MapHub<BesoinsHub>("/hubs/besoins")`

#### 4. Intégration dans les services

**BesoinsService** et **WorkflowService** ont été modifiés pour envoyer des notifications SignalR après chaque ajout d'historique:

```csharp
await besoinsRepo.AddHistoriqueAsync(new Historique { ... });
await besoinsHubService.NotifierHistoriqueAsync(besoinId, action, description);
```

Actions notifiées:
- CREATION
- MODIFICATION
- ENREGISTREMENT
- SOUMISSION
- PIECE_JOINTE
- SUPPRESSION_DOCUMENT
- APPROUVE_ETAPE{N}
- REJETE_ETAPE{N}
- TRANSMISSION

### Frontend (Angular)

#### 1. Service SignalR
**Fichier:** `Frontend/src/app/core/services/signalr.service.ts`

Service singleton qui gère la connexion WebSocket:
- `startConnection()` - Établit la connexion au hub
- `joinBesoinGroup(besoinId)` - Rejoint le groupe d'un besoin
- `leaveBesoinGroup(besoinId)` - Quitte le groupe
- `historiqueUpdates` - Observable des mises à jour reçues

Fonctionnalités:
- Reconnexion automatique en cas de déconnexion
- Transport WebSocket uniquement (skipNegotiation)
- Gestion des erreurs avec retry automatique

#### 2. Intégration dans le composant
**Fichier:** `Frontend/src/app/modules/besoins/besoin-detail/besoin-detail.component.ts`

Le composant s'abonne aux mises à jour lors de l'initialisation:

```typescript
this.signalR.startConnection().then(() => {
  this.signalR.joinBesoinGroup(id);
});

this.signalRSubscription = this.signalR.historiqueUpdates.subscribe(update => {
  if (update.BesoinId === id) {
    // Ajouter la nouvelle entrée en haut de l'historique
    this.historique = [newEntry, ...this.historique];
    this.toastr.info('Historique mis à jour', 'Temps réel');
  }
});
```

Nettoyage dans `ngOnDestroy()`:
```typescript
this.signalR.leaveBesoinGroup(id);
this.signalRSubscription?.unsubscribe();
```

#### 3. Initialisation globale
**Fichier:** `Frontend/src/app/app.component.ts`

La connexion SignalR est initialisée au démarrage de l'application:
```typescript
ngOnInit() {
  this.theme.init();
  this.signalR.startConnection();
}
```

## Installation

### Backend

1. Le package SignalR est déjà inclus dans ASP.NET Core 10.0
2. Restaurer les packages:
```bash
cd Backend/FinAssist.API
dotnet restore
```

### Frontend

1. Installer le package SignalR:
```bash
cd Frontend
npm install
```

2. Le package `@microsoft/signalr` a été ajouté au `package.json`

## Configuration

### Backend - appsettings.json

Aucune configuration spécifique requise. Le hub utilise la même configuration CORS que l'API.

### Frontend - environment.ts

L'URL du hub est construite automatiquement à partir de `apiUrl`:
```typescript
`${environment.apiUrl}/hubs/besoins`
```

## Utilisation

### Scénario d'utilisation typique

1. **Utilisateur A** ouvre le détail d'un besoin (ID: 123)
   - Le composant se connecte au groupe `besoin_123`

2. **Utilisateur B** valide ce besoin
   - Le backend ajoute une entrée dans l'historique
   - Le backend envoie une notification SignalR au groupe `besoin_123`

3. **Utilisateur A** reçoit instantanément la mise à jour
   - L'historique est mis à jour automatiquement
   - Une notification toast s'affiche

### Groupes SignalR

Chaque besoin a son propre groupe: `besoin_{id}`

Avantages:
- Les notifications sont ciblées (seuls les utilisateurs consultant le besoin reçoivent les mises à jour)
- Scalabilité (pas de broadcast global)
- Gestion automatique des connexions/déconnexions

## Tests

### Test manuel

1. Ouvrir deux navigateurs (ou onglets en navigation privée)
2. Se connecter avec deux utilisateurs différents
3. Ouvrir le même besoin dans les deux navigateurs
4. Effectuer une action (validation, ajout de document, etc.) dans un navigateur
5. Vérifier que l'historique se met à jour instantanément dans l'autre navigateur

### Points de vérification

- Console du navigateur: messages `[SignalR] Connexion établie`
- Console du navigateur: messages `[SignalR] Rejoint le groupe besoin_{id}`
- Console du navigateur: messages `[SignalR] Mise à jour historique reçue`
- Notification toast "Historique mis à jour"
- Nouvelle entrée visible en haut de l'historique

## Dépannage

### Problème: Connexion SignalR échoue

**Symptôme:** Erreur dans la console `[SignalR] Erreur de connexion`

**Solutions:**
1. Vérifier que le backend est démarré
2. Vérifier la configuration CORS (`.AllowCredentials()` doit être présent)
3. Vérifier que l'URL du hub est correcte
4. Vérifier les logs du backend pour les erreurs SignalR

### Problème: Les mises à jour ne sont pas reçues

**Symptôme:** Pas de notification toast, historique non mis à jour

**Solutions:**
1. Vérifier que `joinBesoinGroup()` a été appelé
2. Vérifier dans la console que le message "Rejoint le groupe" apparaît
3. Vérifier que le `BesoinId` dans la notification correspond au besoin consulté
4. Vérifier que la subscription n'a pas été unsubscribe prématurément

### Problème: Reconnexion en boucle

**Symptôme:** Messages de connexion/déconnexion répétés

**Solutions:**
1. Vérifier la stabilité du réseau
2. Augmenter le timeout de reconnexion
3. Vérifier les logs du backend pour identifier la cause des déconnexions

## Performance

### Optimisations implémentées

1. **Groupes par besoin** - Évite le broadcast global
2. **Reconnexion automatique** - Gestion transparente des déconnexions
3. **WebSocket uniquement** - Pas de fallback HTTP (skipNegotiation)
4. **Change Detection** - Utilisation de `cdr.detectChanges()` pour forcer la mise à jour

### Considérations de scalabilité

Pour une production à grande échelle:
1. Utiliser Azure SignalR Service ou Redis backplane
2. Implémenter un système de pagination pour l'historique
3. Limiter le nombre de connexions simultanées par utilisateur
4. Ajouter un throttling sur les notifications

## Évolutions futures

### Fonctionnalités possibles

1. **Notification de présence** - Afficher les utilisateurs consultant le besoin
2. **Indicateur de frappe** - Montrer quand un utilisateur rédige un commentaire
3. **Mise à jour du statut** - Actualiser le statut du besoin en temps réel
4. **Notifications push** - Intégrer avec le service de push notifications existant
5. **Historique des connexions** - Logger les connexions/déconnexions pour audit

### Améliorations techniques

1. **Authentification SignalR** - Ajouter un token JWT dans la connexion
2. **Compression** - Activer la compression des messages
3. **Heartbeat** - Configurer un heartbeat personnalisé
4. **Métriques** - Ajouter des métriques de performance (nombre de connexions, latence, etc.)

## Références

- [ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [SignalR JavaScript Client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [@microsoft/signalr npm package](https://www.npmjs.com/package/@microsoft/signalr)
