# Inventaire des classes et m?thodes - FINASSIST

- Classes C#: 185
- Classes TypeScript: 36
- Total classes: 221
- Total m?thodes / constructeurs / accesseurs d?tect?s: 593

## C#

### RequirePermissionAttribute (class)
- Fichier: `Backend\FinAssist.API\Attributes\RequirePermissionAttribute.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `OnAuthorizationAsync(AuthorizationFilterContext context) : Task`

### RoleAuthorizationAttribute (class)
- Fichier: `Backend\FinAssist.API\Attributes\RoleAuthorizationAttribute.cs`
- Ligne de d?claration approx.: 10
- M?thodes:
  - [constructor] `RoleAuthorizationAttribute(params string[] roles)`

### Roles (class)
- Fichier: `Backend\FinAssist.API\Attributes\RoleAuthorizationAttribute.cs`
- Ligne de d?claration approx.: 19
- M?thodes: aucune m?thode d?tect?e

### AuthController (class)
- Fichier: `Backend\FinAssist.API\Controllers\AuthController.cs`
- Ligne de d?claration approx.: 11
- M?thodes:
  - [constructor] `AuthController(IAuthService authService)`
  - [method] `Login([FromBody] LoginRequestDTO request) : Task<IActionResult>`
  - [method] `Logout() : Task<IActionResult>`

### BesoinsController (class)
- Fichier: `Backend\FinAssist.API\Controllers\BesoinsController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `GetAll() : Task<IActionResult>`
  - [method] `GetById(int id) : Task<IActionResult>`
  - [method] `Create([FromForm] CreateBesoinDTO dto, IFormFile? fichier) : Task<IActionResult>`
  - [method] `Update(int id, [FromBody] UpdateBesoinDTO dto) : Task<IActionResult>`
  - [method] `Enregistrer(int id) : Task<IActionResult>`
  - [method] `Soumettre(int id) : Task<IActionResult>`
  - [method] `GetDocuments(int id) : Task<IActionResult>`
  - [method] `SupprimerDocument(int id, int documentId) : Task<IActionResult>`
  - [method] `GetDocumentContenu(int id, int documentId) : Task<IActionResult>`
  - [method] `AjouterPieceJointe(int id, IFormFile fichier) : Task<IActionResult>`
  - [method] `GetHistorique(int id) : Task<IActionResult>`
  - [method] `Delete(int id) : Task<IActionResult>`

### CategoriesController (class)
- Fichier: `Backend\FinAssist.API\Controllers\CategoriesController.cs`
- Ligne de d?claration approx.: 12
- M?thodes:
  - [method] `GetAll() : Task<IActionResult>`
  - [method] `GetById(int id) : Task<IActionResult>`
  - [method] `Create([FromBody] CreateCategorieDTO dto) : Task<IActionResult>`
  - [method] `Update(int id, [FromBody] UpdateCategorieDTO dto) : Task<IActionResult>`
  - [method] `Delete(int id) : Task<IActionResult>`

### LogsController (class)
- Fichier: `Backend\FinAssist.API\Controllers\LogsController.cs`
- Ligne de d?claration approx.: 12
- M?thodes:
  - [method] `GetLogs([FromQuery] FiltreLogsDTO filtres) : Task<IActionResult>`

### MaSignatureController (class)
- Fichier: `Backend\FinAssist.API\Controllers\MaSignatureController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `Get() : Task<IActionResult>`
  - [method] `Save([FromBody] SaveSignatureUtilisateurDTO dto) : Task<IActionResult>`
  - [method] `Delete() : Task<IActionResult>`

### NotificationsController (class)
- Fichier: `Backend\FinAssist.API\Controllers\NotificationsController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `GetMesNotifications() : Task<IActionResult>`
  - [method] `Creer([FromBody] CreateNotificationDTO dto) : Task<IActionResult>`
  - [method] `MarquerLu(int id) : Task<IActionResult>`

### PermissionsController (class)
- Fichier: `Backend\FinAssist.API\Controllers\PermissionsController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `GetAll() : Task<IActionResult>`
  - [method] `GetById(int id) : Task<IActionResult>`
  - [method] `Update(int id, [FromBody] CreatePermissionDTO dto) : Task<IActionResult>`
  - [method] `ToDTO(Permission p) : PermissionDTO`

### ReportingController (class)
- Fichier: `Backend\FinAssist.API\Controllers\ReportingController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `GetStatistiques() : Task<IActionResult>`
  - [method] `GetRapportBesoins([FromQuery] FiltreRapportDTO? filtres) : Task<IActionResult>`
  - [method] `GetDashboard() : Task<IActionResult>`
  - [method] `GetEvolution([FromQuery] string periode = "") : Task<IActionResult>`
  - [method] `Exporter([FromBody] ExportRequestDTO request) : Task<IActionResult>`

### RolesController (class)
- Fichier: `Backend\FinAssist.API\Controllers\RolesController.cs`
- Ligne de d?claration approx.: 14
- M?thodes:
  - [method] `GetAll() : Task<IActionResult>`
  - [method] `GetById(int id) : Task<IActionResult>`
  - [method] `Create([FromBody] CreateRoleDTO dto) : Task<IActionResult>`
  - [method] `Update(int id, [FromBody] CreateRoleDTO dto) : Task<IActionResult>`
  - [method] `Delete(int id) : Task<IActionResult>`
  - [method] `GetPermissions(int id) : Task<IActionResult>`
  - [method] `AddPermissions(int id, [FromBody] AssignerPermissionsDTO dto) : Task<IActionResult>`
  - [method] `SetPermissions(int id, [FromBody] AssignerPermissionsDTO dto) : Task<IActionResult>`
  - [method] `RemovePermission(int id, int permissionId) : Task<IActionResult>`
  - [method] `ToDTO(Role r) : RoleDTO`
  - [method] `ToPermissionDTO(Permission p) : PermissionDTO`

### SignatureController (class)
- Fichier: `Backend\FinAssist.API\Controllers\SignatureController.cs`
- Ligne de d?claration approx.: 12
- M?thodes:
  - [method] `Signer(int documentId) : Task<IActionResult>`
  - [method] `SignerParBesoin(int besoinId, [FromBody] FinAssist.Core.DTOs.Signature.SignerBesoinDTO? dto) : Task<IActionResult>`
  - [method] `GetByBesoin(int besoinId) : Task<IActionResult>`
  - [method] `TelechargerDocumentSigne(int id) : Task<IActionResult>`
  - [method] `Verifier(int id) : Task<IActionResult>`

### UsersController (class)
- Fichier: `Backend\FinAssist.API\Controllers\UsersController.cs`
- Ligne de d?claration approx.: 15
- M?thodes:
  - [method] `GetAll() : Task<IActionResult>`
  - [method] `GetMe() : Task<IActionResult>`
  - [method] `ChangePassword([FromBody] FinAssist.Core.DTOs.Users.ChangePasswordDTO dto) : Task<IActionResult>`
  - [method] `GetById(int id) : Task<IActionResult>`
  - [method] `Create([FromBody] CreateUtilisateurDTO dto) : Task<IActionResult>`
  - [method] `Update(int id, [FromBody] UpdateUtilisateurDTO dto) : Task<IActionResult>`
  - [method] `Deactivate(int id) : Task<IActionResult>`
  - [method] `Activate(int id) : Task<IActionResult>`
  - [method] `Delete(int id) : Task<IActionResult>`
  - [method] `ChangeRole(int id, [FromBody] ChangeRoleDTO dto) : Task<IActionResult>`
  - [method] `GetLogs(int id) : Task<IActionResult>`
  - [method] `GetPermissions(int id) : Task<IActionResult>`
  - [method] `AddPermissions(int id, [FromBody] AssignerPermissionsDTO dto) : Task<IActionResult>`
  - [method] `SetPermissions(int id, [FromBody] AssignerPermissionsDTO dto) : Task<IActionResult>`
  - [method] `RemovePermission(int id, int permissionId) : Task<IActionResult>`

### WorkflowController (class)
- Fichier: `Backend\FinAssist.API\Controllers\WorkflowController.cs`
- Ligne de d?claration approx.: 13
- M?thodes:
  - [method] `Valider(int id, [FromBody] ValiderBesoinDTO dto) : Task<IActionResult>`
  - [method] `Transmettre(int id) : Task<IActionResult>`
  - [method] `GetCircuits() : Task<IActionResult>`
  - [method] `GetCircuit(int id) : Task<IActionResult>`
  - [method] `CreateCircuit([FromBody] CreateWorkflowCircuitDTO dto) : Task<IActionResult>`
  - [method] `UpdateCircuit(int id, [FromBody] UpdateWorkflowCircuitDTO dto) : Task<IActionResult>`
  - [method] `DeleteCircuit(int id) : Task<IActionResult>`

### AuditMiddleware (class)
- Fichier: `Backend\FinAssist.API\Middleware\AuditMiddleware.cs`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [method] `InvokeAsync(HttpContext context,
        ILogService logService,
        IUserAgentParser uaParser,
        IGeoIpService geoIpService) : Task`
  - [method] `DeduireEntite(string path) : string`

### JwtMiddleware (class)
- Fichier: `Backend\FinAssist.API\Middleware\JwtMiddleware.cs`
- Ligne de d?claration approx.: 11
- M?thodes:
  - [constructor] `JwtMiddleware(RequestDelegate next, IConfiguration config)`
  - [method] `InvokeAsync(HttpContext context) : Task`
  - [method] `AttachUserToContext(HttpContext context, string token) : void`

### AuthService (class)
- Fichier: `Backend\FinAssist.Application\Services\AuthService.cs`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [method] `LoginAsync(LoginRequestDTO request) : Task<LoginResponseDTO>`
  - [method] `LogoutAsync(int userId) : Task`

### BesoinsService (class)
- Fichier: `Backend\FinAssist.Application\Services\BesoinsService.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetAllAsync(int utilisateurId, string roleCode) : Task<IEnumerable<BesoinDTO>>`
  - [method] `GetByIdAsync(int id, int utilisateurId, string roleCode) : Task<BesoinDTO>`
  - [method] `CreateAsync(CreateBesoinDTO dto, int utilisateurId) : Task<BesoinDTO>`
  - [method] `UpdateAsync(int id, UpdateBesoinDTO dto, int utilisateurId) : Task<BesoinDTO>`
  - [method] `EnregistrerAsync(int id, int utilisateurId) : Task<BesoinDTO>`
  - [method] `SoumettreAsync(int id, int utilisateurId) : Task<BesoinDTO>`
  - [method] `SupprimerDocumentAsync(int besoinId, int documentId) : Task`
  - [method] `GetDocumentsAsync(int id) : Task<IEnumerable<DocumentDTO>>`
  - [method] `AjouterPieceJointeAsync(int id, string nom, string type, byte[] contenu) : Task<DocumentDTO>`
  - [method] `GetHistoriqueAsync(int id) : Task<IEnumerable<HistoriqueDTO>>`
  - [method] `GetAllCategoriesAsync() : Task<IEnumerable<CategorieDTO>>`
  - [method] `GetCategorieByIdAsync(int id) : Task<CategorieDetailDTO>`
  - [method] `CreateCategorieAsync(CreateCategorieDTO dto) : Task<CategorieDTO>`
  - [method] `AssignerCircuitAsync(int categorieId, int workflowCircuitId) : Task<CategorieDTO>`
  - [method] `UpdateCategorieAsync(int id, UpdateCategorieDTO dto) : Task<CategorieDTO>`
  - [method] `DeleteCategorieAsync(int id) : Task`
  - [method] `DeleteAsync(int id) : Task`
  - [method] `UtilisateurPeutAgirSurEtape(EtapeCircuit etape, string roleCode, HashSet<string> permissions) : bool`
  - [method] `ToDTO(Besoin b) : BesoinDTO`
  - [method] `ToCategorieDTO(Categorie c) : CategorieDTO`

### Roles (class)
- Fichier: `Backend\FinAssist.Application\Services\BesoinsService.cs`
- Ligne de d?claration approx.: 611
- M?thodes: aucune m?thode d?tect?e

### BesoinIdComparer (class)
- Fichier: `Backend\FinAssist.Application\Services\BesoinsService.cs`
- Ligne de d?claration approx.: 619
- M?thodes:
  - [method] `Equals(Besoin? x, Besoin? y) : bool`
  - [method] `GetHashCode(Besoin obj) : int`

### LogService (class)
- Fichier: `Backend\FinAssist.Application\Services\LogService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `LoggerAsync(string action,
        string entiteType,
        int? entiteId = null,
        string? ancienneValeur = null,
        string? nouvelleValeur = null,
        int? utilisateurId = null,
        string? adresseIp = null,
        string? systemeExploitation = null,
        string? navigateur = null,
        string? pays = null,
        string? ville = null) : Task`

### NotificationService (class)
- Fichier: `Backend\FinAssist.Application\Services\NotificationService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `GetMesNotificationsAsync(int utilisateurId) : Task<IEnumerable<NotificationDTO>>`
  - [method] `MarquerLuAsync(int notificationId, int utilisateurId) : Task`
  - [method] `CreerEtEnvoyerAsync(CreateNotificationDTO dto) : Task<NotificationDTO>`
  - [method] `NotifierSoumissionAsync(int besoinId, string titreBesoin, int soumetteurId, string roleEtape1, string nomSoumetteur) : Task`
  - [method] `NotifierTransmissionAsync(int besoinId, string titreBesoin, string roleProchaineEtape, string nomTransmetteur) : Task`
  - [method] `NotifierRejetAsync(int besoinId, string titreBesoin, int createurId, string roleEtape) : Task`
  - [method] `NotifierSignatureAsync(int besoinId, string titreBesoin, int agentId, string roleSignataire) : Task`

### PermissionService (class)
- Fichier: `Backend\FinAssist.Application\Services\PermissionService.cs`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [method] `CacheKey(int userId) : string`
  - [method] `GetPermissionsEffectivesAsync(int utilisateurId) : Task<IEnumerable<string>>`
  - [method] `HasPermissionAsync(int utilisateurId, string permissionCode) : Task<bool>`
  - [method] `InvalidateCache(int utilisateurId) : void`

### ReportingService (class)
- Fichier: `Backend\FinAssist.Application\Services\ReportingService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `GetStatistiquesAsync() : Task<StatistiquesDTO>`
  - [method] `GetRapportBesoinsAsync(FiltreRapportDTO? filtres, int generateurId) : Task<RapportDTO>`
  - [method] `GetDashboardAsync(int utilisateurId, string roleCode) : Task<DashboardDTO>`
  - [method] `GetEvolutionBesoinsAsync(string periode, int utilisateurId, string roleCode) : Task<IEnumerable<EvolutionPointDTO>>`
  - [method] `UtilisateurPeutAgirSurEtape(EtapeCircuit etape, string roleCode, HashSet<string> permissions) : bool`

### BesoinIdEqualityComparer (class)
- Fichier: `Backend\FinAssist.Application\Services\ReportingService.cs`
- Ligne de d?claration approx.: 440
- M?thodes:
  - [method] `Equals(Besoin? x, Besoin? y) : bool`
  - [method] `GetHashCode(Besoin obj) : int`

### SignatureService (class)
- Fichier: `Backend\FinAssist.Application\Services\SignatureService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `SignerAsync(int documentId, int utilisateurId) : Task<SignatureDTO>`
  - [method] `SignerParBesoinAsync(int besoinId, int utilisateurId, SignerBesoinDTO? dto = null) : Task<SignatureDTO>`
  - [method] `SignerDocumentAsync(Document document, int utilisateurId, SignerBesoinDTO? dto = null) : Task<SignatureDTO>`
  - [method] `VerifierAsync(int signatureId) : Task<VerificationDTO>`
  - [method] `GetApercuAsync(int besoinId) : Task<SignatureApercuDTO>`
  - [method] `ToDTO(SignatureElectronique s) : SignatureDTO`

### SignatureUtilisateurService (class)
- Fichier: `Backend\FinAssist.Application\Services\SignatureUtilisateurService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `SauvegarderAsync(int utilisateurId, SaveSignatureUtilisateurDTO dto) : Task<SignatureUtilisateurDTO>`
  - [method] `SupprimerAsync(int utilisateurId) : Task`
  - [method] `ToDTO(SignatureUtilisateur s) : SignatureUtilisateurDTO`

### UsersService (class)
- Fichier: `Backend\FinAssist.Application\Services\UsersService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `GetAllAsync() : Task<IEnumerable<UtilisateurDTO>>`
  - [method] `GetByIdAsync(int id) : Task<UtilisateurDTO>`
  - [method] `CreateAsync(CreateUtilisateurDTO dto) : Task<UtilisateurDTO>`
  - [method] `GeneratePassword() : string`
  - [method] `BuildWelcomeEmail(string prenom, string nom, string email, string password) : string`
  - [method] `UpdateAsync(int id, UpdateUtilisateurDTO dto) : Task<UtilisateurDTO>`
  - [method] `DeactivateAsync(int id) : Task`
  - [method] `ActivateAsync(int id) : Task`
  - [method] `DeleteAsync(int id) : Task`
  - [method] `ChangeRoleAsync(int id, int roleId) : Task<UtilisateurDTO>`
  - [method] `GetLogsAsync(int id) : Task<IEnumerable<LogUtilisateurDTO>>`
  - [method] `ChangePasswordAsync(int id, string ancienMotDePasse, string nouveauMotDePasse) : Task`
  - [method] `ToDTO(Utilisateur u) : UtilisateurDTO`

### WorkflowEngine (class)
- Fichier: `Backend\FinAssist.Application\Services\WorkflowEngine.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `StatutEnAttente(string roleRequis) : string`
  - [method] `StatutApprouve(string roleRequis) : string`
  - [method] `StatutSigne(string roleRequis) : string`
  - [method] `StatutRejete(string roleRequis) : string`
  - [method] `EstEnAttente(string statut) : bool`
  - [method] `EstApprouve(string statut) : bool`
  - [method] `EstRejete(string statut) : bool`
  - [method] `EstSigne(string statut) : bool`
  - [method] `VerifierRole(EtapeCircuit etape, string roleCode, HashSet<string>? permissions = null) : void`

### WorkflowService (class)
- Fichier: `Backend\FinAssist.Application\Services\WorkflowService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `ValiderAsync(int besoinId, ValiderBesoinDTO dto, int validateurId, string roleCode, string nomValidateur) : Task<ValidationDTO>`
  - [method] `TransmettreAsync(int besoinId, int validateurId, string nomTransmetteur) : Task<ValidationDTO>`
  - [method] `GetCircuitByIdAsync(int id) : Task<WorkflowCircuitDTO>`
  - [method] `GetAllCircuitsAsync() : Task<IEnumerable<WorkflowCircuitDTO>>`
  - [method] `CreateCircuitAsync(CreateWorkflowCircuitDTO dto, string nomCreateur) : Task<WorkflowCircuitDTO>`
  - [method] `UpdateCircuitAsync(int id, UpdateWorkflowCircuitDTO dto) : Task<WorkflowCircuitDTO>`
  - [method] `DeleteCircuitAsync(int id) : Task`
  - [method] `GetCircuitDuBesoinAsync(Besoin besoin) : Task<WorkflowCircuit>`
  - [method] `ToValidationDTO(Validation v) : ValidationDTO`
  - [method] `ToCircuitDTO(WorkflowCircuit c) : WorkflowCircuitDTO`

### LoginRequestDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Auth\LoginRequestDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### LoginResponseDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Auth\LoginResponseDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UtilisateurInfoDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Auth\UtilisateurInfoDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### BesoinDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\BesoinDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CategorieDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\CategorieDTO.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### CategorieDetailDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\CategorieDTO.cs`
- Ligne de d?claration approx.: 15
- M?thodes: aucune m?thode d?tect?e

### CreateBesoinDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\CreateBesoinDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CreateCategorieDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\CreateCategorieDTO.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### DocumentDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\DocumentDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### HistoriqueDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\HistoriqueDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UpdateBesoinDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\UpdateBesoinDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UpdateCategorieDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Besoins\UpdateCategorieDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### FiltreLogsDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Logs\FiltreLogsDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### LogActiviteDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Logs\LogActiviteDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CreateNotificationDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Notifications\CreateNotificationDTO.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### NotificationDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Notifications\NotificationDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CreatePermissionDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Permissions\CreatePermissionDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### PermissionDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Permissions\PermissionDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### PermissionsEffectivesDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Permissions\PermissionsEffectivesDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### DashboardDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\DashboardDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### BesoinRecent (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\DashboardDTO.cs`
- Ligne de d?claration approx.: 17
- M?thodes: aucune m?thode d?tect?e

### EvolutionPointDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\EvolutionPointDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### ExportRequestDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\ExportRequestDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### FiltreRapportDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\FiltreRapportDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### RapportDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\RapportDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### StatistiquesDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Reporting\StatistiquesDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### AssignerPermissionsDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Roles\AssignerPermissionsDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CreateRoleDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Roles\CreateRoleDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### RoleDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Roles\RoleDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignatureApercuDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signature\SignatureApercuDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignataireInfoDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signature\SignatureApercuDTO.cs`
- Ligne de d?claration approx.: 17
- M?thodes: aucune m?thode d?tect?e

### SignatureDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signature\SignatureDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignerBesoinDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signature\SignerBesoinDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### VerificationDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signature\VerificationDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignatureUtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signatures\SignatureUtilisateurDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SaveSignatureUtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Signatures\SignatureUtilisateurDTO.cs`
- Ligne de d?claration approx.: 13
- M?thodes: aucune m?thode d?tect?e

### ChangePasswordDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\ChangePasswordDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### ChangeRoleDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\ChangeRoleDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### CreateUtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\CreateUtilisateurDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### LogUtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\LogUtilisateurDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UpdateUtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\UpdateUtilisateurDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UtilisateurDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Users\UtilisateurDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### EtapeCircuitDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\EtapeCircuitDTO.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### CreateEtapeCircuitDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\EtapeCircuitDTO.cs`
- Ligne de d?claration approx.: 19
- M?thodes: aucune m?thode d?tect?e

### StatutWorkflowDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\StatutWorkflowDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UpdateWorkflowCircuitDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\UpdateWorkflowCircuitDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### ValidationDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\ValidationDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### ValiderBesoinDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\ValiderBesoinDTO.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### WorkflowCircuitDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\WorkflowCircuitDTO.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### CreateWorkflowCircuitDTO (class)
- Fichier: `Backend\FinAssist.Core\DTOs\Workflow\WorkflowCircuitDTO.cs`
- Ligne de d?claration approx.: 16
- M?thodes: aucune m?thode d?tect?e

### Besoin (class)
- Fichier: `Backend\FinAssist.Core\Entities\Besoin.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Categorie (class)
- Fichier: `Backend\FinAssist.Core\Entities\Categorie.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Document (class)
- Fichier: `Backend\FinAssist.Core\Entities\Document.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### EtapeCircuit (class)
- Fichier: `Backend\FinAssist.Core\Entities\EtapeCircuit.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### Historique (class)
- Fichier: `Backend\FinAssist.Core\Entities\Historique.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### LogActivite (class)
- Fichier: `Backend\FinAssist.Core\Entities\LogActivite.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### LogUtilisateur (class)
- Fichier: `Backend\FinAssist.Core\Entities\LogUtilisateur.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Notification (class)
- Fichier: `Backend\FinAssist.Core\Entities\Notification.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Permission (class)
- Fichier: `Backend\FinAssist.Core\Entities\Permission.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Role (class)
- Fichier: `Backend\FinAssist.Core\Entities\Role.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### RolePermission (class)
- Fichier: `Backend\FinAssist.Core\Entities\RolePermission.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignatureElectronique (class)
- Fichier: `Backend\FinAssist.Core\Entities\SignatureElectronique.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### SignatureUtilisateur (class)
- Fichier: `Backend\FinAssist.Core\Entities\SignatureUtilisateur.cs`
- Ligne de d?claration approx.: 6
- M?thodes: aucune m?thode d?tect?e

### Utilisateur (class)
- Fichier: `Backend\FinAssist.Core\Entities\Utilisateur.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UtilisateurNotification (class)
- Fichier: `Backend\FinAssist.Core\Entities\UtilisateurNotification.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### UtilisateurPermission (class)
- Fichier: `Backend\FinAssist.Core\Entities\UtilisateurPermission.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### Validation (class)
- Fichier: `Backend\FinAssist.Core\Entities\Validation.cs`
- Ligne de d?claration approx.: 3
- M?thodes: aucune m?thode d?tect?e

### WorkflowCircuit (class)
- Fichier: `Backend\FinAssist.Core\Entities\WorkflowCircuit.cs`
- Ligne de d?claration approx.: 5
- M?thodes: aucune m?thode d?tect?e

### AppDbContext (class)
- Fichier: `Backend\FinAssist.Infrastructure\Data\AppDbContext.cs`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [method] `OnModelCreating(ModelBuilder modelBuilder) : void`

### DataSeeder (class)
- Fichier: `Backend\FinAssist.Infrastructure\Data\DataSeeder.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `SeedAsync(IServiceProvider services) : Task`

### M001_InitialCreate (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317144443_M001_InitialCreate.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M001_InitialCreate (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317144443_M001_InitialCreate.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M002_AddPermissionAndUpdateEntities (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317152707_M002_AddPermissionAndUpdateEntities.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M002_AddPermissionAndUpdateEntities (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317152707_M002_AddPermissionAndUpdateEntities.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M003_RemoveRefreshToken (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317155326_M003_RemoveRefreshToken.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M003_RemoveRefreshToken (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317155326_M003_RemoveRefreshToken.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M003_UsersModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317162907_M003_UsersModule.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M003_UsersModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260317162907_M003_UsersModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M004_RestructureEntities (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319092947_M004_RestructureEntities.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M004_RestructureEntities (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319092947_M004_RestructureEntities.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M005_AddLogUtilisateur (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319095252_M005_AddLogUtilisateur.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M005_AddLogUtilisateur (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319095252_M005_AddLogUtilisateur.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M006_AddBesoinsModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319102056_M006_AddBesoinsModule.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M006_AddBesoinsModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319102056_M006_AddBesoinsModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M006_AddBesoinsAndWorkflow (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319103001_M006_AddBesoinsAndWorkflow.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M006_AddBesoinsAndWorkflow (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319103001_M006_AddBesoinsAndWorkflow.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M007_AddWorkflowModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319103140_M007_AddWorkflowModule.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M007_AddWorkflowModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319103140_M007_AddWorkflowModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M008_AddSignatureModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319104015_M008_AddSignatureModule.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M008_AddSignatureModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319104015_M008_AddSignatureModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M009_AddNotificationsModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319135915_M009_AddNotificationsModule.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M009_AddNotificationsModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319135915_M009_AddNotificationsModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddLogActivite (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319141113_AddLogActivite.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddLogActivite (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260319141113_AddLogActivite.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### CategorieWorkflowRequired (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321000255_CategorieWorkflowRequired.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### CategorieWorkflowRequired (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321000255_CategorieWorkflowRequired.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddSignee1Signee2 (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321065552_AddSignee1Signee2.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddSignee1Signee2 (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321065552_AddSignee1Signee2.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddEnAttente (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321072526_AddEnAttente.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddEnAttente (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321072526_AddEnAttente.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### RefactorStatuts (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321082253_RefactorStatuts.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### RefactorStatuts (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321082253_RefactorStatuts.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddWorkflowEtapes (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321090710_AddWorkflowEtapes.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddWorkflowEtapes (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321090710_AddWorkflowEtapes.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddWorkflowNomUnique (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321095012_AddWorkflowNomUnique.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddWorkflowNomUnique (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321095012_AddWorkflowNomUnique.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddDynamicWorkflowCircuit (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321174244_AddDynamicWorkflowCircuit.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddDynamicWorkflowCircuit (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321174244_AddDynamicWorkflowCircuit.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddDynamicWorkflowCircuitV2 (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321174529_AddDynamicWorkflowCircuitV2.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddDynamicWorkflowCircuitV2 (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321174529_AddDynamicWorkflowCircuitV2.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddCategorieWorkflowCircuitId (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321184003_AddCategorieWorkflowCircuitId.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddCategorieWorkflowCircuitId (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321184003_AddCategorieWorkflowCircuitId.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### RemoveWorkflowAndCleanup (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321190108_RemoveWorkflowAndCleanup.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### RemoveWorkflowAndCleanup (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321190108_RemoveWorkflowAndCleanup.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddCategorieNomUnique (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321192025_AddCategorieNomUnique.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddCategorieNomUnique (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260321192025_AddCategorieNomUnique.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### SignatureMultipleSignataires (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260322064232_SignatureMultipleSignataires.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### SignatureMultipleSignataires (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260322064232_SignatureMultipleSignataires.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddPermissionFonctionnaliteModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260322092102_AddPermissionFonctionnaliteModule.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddPermissionFonctionnaliteModule (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260322092102_AddPermissionFonctionnaliteModule.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### RemoveAgentId (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260323112041_RemoveAgentId.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### RemoveAgentId (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260323112041_RemoveAgentId.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### RemoveAdresseIp (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260325172550_RemoveAdresseIp.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### RemoveAdresseIp (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260325172550_RemoveAdresseIp.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddSignatureManuscrite (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260326210601_AddSignatureManuscrite.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddSignatureManuscrite (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260326210601_AddSignatureManuscrite.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AddPdfSigneToSignature (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260328180151_AddPdfSigneToSignature.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### AddPdfSigneToSignature (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260328180151_AddPdfSigneToSignature.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M015_AddLogsMetadata (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260409123529_M015_AddLogsMetadata.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M015_AddLogsMetadata (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260409123529_M015_AddLogsMetadata.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### M016_AddSignatureUtilisateur (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260409150112_M016_AddSignatureUtilisateur.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `Up(MigrationBuilder migrationBuilder) : void`
  - [method] `Down(MigrationBuilder migrationBuilder) : void`

### M016_AddSignatureUtilisateur (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\20260409150112_M016_AddSignatureUtilisateur.Designer.cs`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [method] `BuildTargetModel(ModelBuilder modelBuilder) : void`

### AppDbContextModelSnapshot (class)
- Fichier: `Backend\FinAssist.Infrastructure\Migrations\AppDbContextModelSnapshot.cs`
- Ligne de d?claration approx.: 14
- M?thodes:
  - [method] `BuildModel(ModelBuilder modelBuilder) : void`

### AuthRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\AuthRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes: aucune m?thode d?tect?e

### BesoinsRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\BesoinsRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetAllAsync() : Task<IEnumerable<Besoin>>`
  - [method] `GetByUtilisateurAsync(int utilisateurId) : Task<IEnumerable<Besoin>>`
  - [method] `GetValidationsParUtilisateurAsync(int utilisateurId) : Task<IEnumerable<Validation>>`
  - [method] `GetBesoinIdsValidesParUtilisateurAsync(int utilisateurId) : Task<IEnumerable<int>>`
  - [method] `CreateAsync(Besoin besoin) : Task<Besoin>`
  - [method] `UpdateAsync(Besoin besoin) : Task<Besoin>`
  - [method] `AddHistoriqueAsync(Historique historique) : Task`
  - [method] `GetHistoriqueAsync(int besoinId) : Task<IEnumerable<Historique>>`
  - [method] `AddDocumentAsync(Document document) : Task`
  - [method] `GetDocumentsAsync(int besoinId) : Task<IEnumerable<Document>>`
  - [method] `GetAllCategoriesAsync() : Task<IEnumerable<Categorie>>`
  - [method] `CategorieNomExistsAsync(string nom, int? excludeId = null) : Task<bool>`
  - [method] `CreateCategorieAsync(Categorie categorie) : Task<Categorie>`
  - [method] `UpdateCategorieAsync(Categorie categorie) : Task<Categorie>`
  - [method] `DeleteCategorieAsync(Categorie categorie) : Task`
  - [method] `DeleteDocumentAsync(int documentId) : Task`
  - [method] `DeleteBesoinAsync(Besoin besoin) : Task`
  - [method] `UtilisateurADejaValideAsync(int besoinId, int utilisateurId) : Task<bool>`
  - [method] `GetCodesPermissionsUtilisateurAsync(int utilisateurId) : Task<IEnumerable<string>>`

### LogRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\LogRepository.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `AjouterAsync(LogActivite log) : Task`

### NotificationRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\NotificationRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `CreateAsync(Notification notification, IEnumerable<int> destinataireIds) : Task<Notification>`
  - [method] `GetByUtilisateurAsync(int utilisateurId) : Task<IEnumerable<UtilisateurNotification>>`
  - [method] `MarquerLuAsync(int notificationId, int utilisateurId) : Task`
  - [method] `GetUtilisateurIdsByRoleAsync(string roleCode) : Task<IEnumerable<int>>`

### PermissionsRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\PermissionsRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetAllAsync() : Task<IEnumerable<Permission>>`
  - [method] `ExistsAsync(string code) : Task<bool>`
  - [method] `CreateAsync(Permission permission) : Task<Permission>`
  - [method] `UpdateAsync(Permission permission) : Task<Permission>`
  - [method] `DeleteAsync(int id) : Task`
  - [method] `AssignToRoleAsync(int permissionId, int roleId) : Task`
  - [method] `RemoveFromRoleAsync(int permissionId, int roleId) : Task`
  - [method] `SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds) : Task`
  - [method] `GetPermissionsByRoleAsync(int roleId) : Task<IEnumerable<Permission>>`
  - [method] `AssignToUtilisateurAsync(int permissionId, int utilisateurId) : Task`
  - [method] `RemoveFromUtilisateurAsync(int permissionId, int utilisateurId) : Task`
  - [method] `SetUtilisateurPermissionsAsync(int utilisateurId, IEnumerable<int> permissionIds) : Task`
  - [method] `GetPermissionsDirectesAsync(int utilisateurId) : Task<IEnumerable<Permission>>`
  - [method] `GetPermissionsEffectivesAsync(int utilisateurId) : Task<IEnumerable<string>>`

### ReportingRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\ReportingRepository.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `GetAllBesoinsAsync() : Task<IEnumerable<Besoin>>`
  - [method] `GetBesoinsFiltrésAsync(FiltreRapportDTO? filtres) : Task<IEnumerable<Besoin>>`
  - [method] `GetBesoinsParUtilisateurAsync(int utilisateurId) : Task<IEnumerable<Besoin>>`
  - [method] `GetAllUtilisateursAsync() : Task<IEnumerable<Utilisateur>>`
  - [method] `CountSignaturesAsync() : Task<int>`
  - [method] `CountNotificationsAsync() : Task<int>`
  - [method] `CountNotificationsNonLuesAsync(int utilisateurId) : Task<int>`

### RolesRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\RolesRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetAllAsync() : Task<IEnumerable<Role>>`
  - [method] `ExistsAsync(string code) : Task<bool>`
  - [method] `CreateAsync(Role role) : Task<Role>`
  - [method] `UpdateAsync(Role role) : Task<Role>`
  - [method] `DeleteAsync(int id) : Task`

### SignatureRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\SignatureRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `AddAsync(SignatureElectronique signature) : Task<SignatureElectronique>`
  - [method] `UpdateAsync(SignatureElectronique signature) : Task<SignatureElectronique>`

### SignatureUtilisateurRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\SignatureUtilisateurRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `SaveAsync(SignatureUtilisateur signature) : Task<SignatureUtilisateur>`
  - [method] `DeleteAsync(int utilisateurId) : Task`

### UsersRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\UsersRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetAllAsync() : Task<IEnumerable<Utilisateur>>`
  - [method] `CreateAsync(Utilisateur utilisateur) : Task<Utilisateur>`
  - [method] `UpdateAsync(Utilisateur utilisateur) : Task<Utilisateur>`
  - [method] `DeleteAsync(int id) : Task`
  - [method] `DeleteCascadeAsync(int id) : Task`
  - [method] `GetLogsAsync(int utilisateurId) : Task<IEnumerable<LogUtilisateur>>`
  - [method] `AddLogAsync(LogUtilisateur log) : Task`
  - [method] `HasActionsAsync(int utilisateurId) : Task<bool>`
  - [method] `GetIdsAvecActionsAsync() : Task<HashSet<int>>`

### WorkflowRepository (class)
- Fichier: `Backend\FinAssist.Infrastructure\Repositories\WorkflowRepository.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `GetValidationsByBesoinAsync(int besoinId) : Task<IEnumerable<Validation>>`
  - [method] `AddValidationAsync(Validation validation) : Task<Validation>`
  - [method] `GetAllCircuitsAsync() : Task<IEnumerable<WorkflowCircuit>>`
  - [method] `CircuitNomExistsAsync(string nom, int? excludeId = null) : Task<bool>`
  - [method] `CreateCircuitAsync(WorkflowCircuit circuit) : Task<WorkflowCircuit>`
  - [method] `UpdateCircuitAsync(WorkflowCircuit circuit) : Task<WorkflowCircuit>`
  - [method] `DeleteCircuitAsync(WorkflowCircuit circuit) : Task`

### Argon2PasswordService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\Argon2PasswordService.cs`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [method] `Hash(string password) : string`
  - [method] `Verify(string password, string hash) : bool`

### ExportService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\ExportService.cs`
- Ligne de d?claration approx.: 11
- M?thodes: aucune m?thode d?tect?e

### FirebaseNotificationService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\FirebaseNotificationService.cs`
- Ligne de d?claration approx.: 10
- M?thodes:
  - [method] `SendAsync(IEnumerable<int> utilisateurIds, string titre, string corps) : Task`

### GeoIpService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\GeoIpService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `IpApiResponse(string? Status, string? Country, string? City) : record`

### HashingService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\HashingService.cs`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [method] `ComputeHash(byte[] data) : string`
  - [method] `Sign(string empreinte, string secret) : string`
  - [method] `Verify(string empreinte, string valeurSignature, string secret) : bool`

### JwtTokenService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\JwtTokenService.cs`
- Ligne de d?claration approx.: 11
- M?thodes:
  - [method] `GenerateAccessToken(Utilisateur user, IEnumerable<string> permissions) : string`

### PdfSignatureService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\PdfSignatureService.cs`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [method] `NormalizeBase64(string signatureBase64) : string`

### SignatureConfig (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\SignatureConfig.cs`
- Ligne de d?claration approx.: 6
- M?thodes: aucune m?thode d?tect?e

### SmtpEmailService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\SmtpEmailService.cs`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [method] `SendAsync(string to, string subject, string htmlBody) : Task<bool>`

### UserAgentParserService (class)
- Fichier: `Backend\FinAssist.Infrastructure\Services\UserAgentParserService.cs`
- Ligne de d?claration approx.: 6
- M?thodes: aucune m?thode d?tect?e

## TypeScript

### AppComponent (class)
- Fichier: `Frontend\src\app\app.component.ts`
- Ligne de d?claration approx.: 11
- M?thodes:
  - [constructor] `constructor(private theme: ThemeService)`
  - [method] `ngOnInit()`

### AuthService (class)
- Fichier: `Frontend\src\app\core\services\auth.service.ts`
- Ligne de d?claration approx.: 9
- M?thodes:
  - [constructor] `constructor(private http: HttpClient, private router: Router)`
  - [method] `login(credentials: LoginRequest)`
  - [method] `logout()`

### BesoinsService (class)
- Fichier: `Frontend\src\app\core\services\besoins.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getAll()`
  - [method] `getById(id: number)`
  - [method] `create(dto: CreateBesoinDTO, fichier?: File)`
  - [method] `update(id: number, dto: UpdateBesoinDTO)`
  - [method] `delete(id: number)`
  - [method] `enregistrer(id: number)`
  - [method] `soumettre(id: number)`
  - [method] `getHistorique(id: number)`
  - [method] `getDocuments(id: number)`
  - [method] `ajouterPieceJointe(id: number, fichier: File)`
  - [method] `supprimerDocument(besoinId: number, documentId: number)`

### CategoriesService (class)
- Fichier: `Frontend\src\app\core\services\categories.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getAll()`
  - [method] `getById(id: number)`
  - [method] `create(dto: CreateCategorieDTO)`
  - [method] `update(id: number, dto: UpdateCategorieDTO)`
  - [method] `delete(id: number)`

### LogsService (class)
- Fichier: `Frontend\src\app\core\services\logs.service.ts`
- Ligne de d?claration approx.: 6
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getLogs(page = 1, pageSize = 20, filtres?: { action?: string; dateDebut?: string; dateFin?: string })`

### MaSignatureService (class)
- Fichier: `Frontend\src\app\core\services\ma-signature.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `get()`
  - [method] `save(dto: SaveSignatureUtilisateurDTO)`
  - [method] `delete()`

### NotificationsService (class)
- Fichier: `Frontend\src\app\core\services\notifications.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getMesNotifications()`
  - [method] `marquerLu(id: number)`
  - [method] `envoyer(dto: CreateNotificationDTO)`

### PermissionsService (class)
- Fichier: `Frontend\src\app\core\services\permissions.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getAll()`
  - [method] `getById(id: number)`
  - [method] `update(id: number, dto: Partial<PermissionDTO>)`

### ReportingService (class)
- Fichier: `Frontend\src\app\core\services\reporting.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getDashboard()`
  - [method] `getEvolutionBesoins(periode: '' | '' | '')`
  - [method] `getStatistiques()`
  - [method] `getRapportBesoins(filtres?: FiltreRapportDTO)`
  - [method] `exporter(request: ExportRequestDTO)`

### RolesService (class)
- Fichier: `Frontend\src\app\core\services\roles.service.ts`
- Ligne de d?claration approx.: 7
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getAll()`
  - [method] `getById(id: number)`
  - [method] `create(dto: CreateRoleDTO)`
  - [method] `update(id: number, dto: CreateRoleDTO)`
  - [method] `delete(id: number)`
  - [method] `getPermissions(id: number)`
  - [method] `addPermissions(id: number, dto: AssignerPermissionsDTO)`
  - [method] `setPermissions(id: number, dto: AssignerPermissionsDTO)`
  - [method] `removePermission(id: number, permId: number)`

### SignaturesService (class)
- Fichier: `Frontend\src\app\core\services\signatures.service.ts`
- Ligne de d?claration approx.: 29
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `signerParBesoin(besoinId: number, payload?: SignerBesoinPayload)`
  - [method] `signerParDocument(documentId: number)`
  - [method] `verifier(id: number)`
  - [method] `getApercuBesoin(besoinId: number)`
  - [method] `telechargerDocumentSigne(signatureId: number)`

### ThemeService (class)
- Fichier: `Frontend\src\app\core\services\theme.service.ts`
- Ligne de d?claration approx.: 4
- M?thodes:
  - [method] `toggle()`
  - [method] `apply(dark: boolean)`
  - [method] `init()`

### UsersService (class)
- Fichier: `Frontend\src\app\core\services\users.service.ts`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getMe()`
  - [method] `changePassword(dto: { ancienMotDePasse: string; nouveauMotDePasse: string })`
  - [method] `getAll()`
  - [method] `getById(id: number)`
  - [method] `create(dto: CreateUtilisateurDTO)`
  - [method] `update(id: number, dto: UpdateUtilisateurDTO)`
  - [method] `deactivate(id: number)`
  - [method] `activate(id: number)`
  - [method] `deletePermanent(id: number)`
  - [method] `changeRole(id: number, roleId: number)`
  - [method] `getLogs(id: number)`
  - [method] `getPermissions(id: number)`
  - [method] `addPermissions(id: number, dto: AssignerPermissionsDTO)`
  - [method] `setPermissions(id: number, dto: AssignerPermissionsDTO)`
  - [method] `removePermission(id: number, permId: number)`

### WorkflowService (class)
- Fichier: `Frontend\src\app\core\services\workflow.service.ts`
- Ligne de d?claration approx.: 8
- M?thodes:
  - [constructor] `constructor(private http: HttpClient)`
  - [method] `getCircuits()`
  - [method] `getCircuit(id: number)`
  - [method] `createCircuit(dto: CreateWorkflowCircuitDTO)`
  - [method] `updateCircuit(id: number, dto: UpdateWorkflowCircuitDTO)`
  - [method] `deleteCircuit(id: number)`
  - [method] `valider(id: number, dto: ValiderBesoinDTO)`
  - [method] `transmettre(id: number)`

### PermissionsComponent (class)
- Fichier: `Frontend\src\app\modules\admin\permissions\permissions.component.ts`
- Ligne de d?claration approx.: 15
- M?thodes:
  - [constructor] `constructor(public auth: AuthService, private permissionsService: PermissionsService, private fb: FormBuilder)`
  - [method] `ngOnInit()`
  - [method] `openEdit(p: PermissionDTO)`
  - [method] `submit()`

### RolesComponent (class)
- Fichier: `Frontend\src\app\modules\admin\roles\roles.component.ts`
- Ligne de d?claration approx.: 17
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private rolesService: RolesService,
    private permissionsService: PermissionsService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `openPermissions(r: RoleDTO)`
  - [method] `fermerPermModal()`
  - [method] `togglePerm(permId: number, event: Event)`
  - [method] `toggleModule(permissions: PermissionDTO[])`
  - [method] `sauvegarderPermissions()`
  - [method] `openCreate()`
  - [method] `openEdit(r: RoleDTO)`
  - [method] `fermerModal()`
  - [method] `submit()`
  - [method] `delete(id: number)`

### UsersComponent (class)
- Fichier: `Frontend\src\app\modules\admin\users\users.component.ts`
- Ligne de d?claration approx.: 27
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private usersService: UsersService,
    private rolesService: RolesService,
    private permissionsService: PermissionsService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `openPermissions(u: UtilisateurDTO)`
  - [method] `fermerPermModal()`
  - [method] `toggleDirecte(permId: number, event: Event)`
  - [method] `toggleModule(permissions: PermissionDTO[])`
  - [method] `sauvegarderPermissions()`
  - [method] `openCreate()`
  - [method] `openEdit(u: UtilisateurDTO)`
  - [method] `fermerModal()`
  - [method] `submit()`
  - [method] `deactivate(id: number)`
  - [method] `activate(id: number)`
  - [method] `deletePermanent(id: number)`

### LoginComponent (class)
- Fichier: `Frontend\src\app\modules\auth\login\login.component.ts`
- Ligne de d?claration approx.: 14
- M?thodes:
  - [constructor] `constructor(private fb: FormBuilder, private auth: AuthService, private router: Router)`
  - [method] `submit()`

### BesoinDetailComponent (class)
- Fichier: `Frontend\src\app\modules\besoins\besoin-detail\besoin-detail.component.ts`
- Ligne de d?claration approx.: 40
- M?thodes:
  - [constructor] `constructor(private route: ActivatedRoute,
    private router: Router,
    public besoinsService: BesoinsService,
    private workflowService: WorkflowService,
    private signaturesService: SignaturesService,
    private maSignatureService: MaSignatureService,
    public auth: AuthService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef)`
  - [method] `ngOnInit()`
  - [method] `ngOnDestroy()`
  - [method] `onPdfLoaded(pdf: any)`
  - [method] `onPdfError(error: any)`
  - [method] `onFichierChange(event: Event)`
  - [method] `supprimerDocument(docId: number)`
  - [method] `enregistrer()`
  - [method] `soumettre()`
  - [method] `valider()`
  - [method] `transmettre()`
  - [method] `ouvrirSignature()`
  - [method] `_chargerDocumentPourSignature()`
  - [method] `fermerSignModal()`
  - [method] `onDocClick(event: MouseEvent)`
  - [method] `confirmerPlacement()`
  - [method] `retourEtape1()`
  - [method] `initCanvas()`
  - [method] `onCanvasMouseDown(e: MouseEvent)`
  - [method] `onCanvasMouseMove(e: MouseEvent)`
  - [method] `onCanvasMouseUp()`
  - [method] `onCanvasTouchStart(e: TouchEvent)`
  - [method] `onCanvasTouchMove(e: TouchEvent)`
  - [method] `effacerCanvas()`
  - [method] `validerSignature()`
  - [method] `ouvrirApercu()`
  - [method] `_chargerDocumentOriginal()`
  - [method] `fermerApercu()`
  - [method] `telecharger()`
  - [method] `zoomIn()`
  - [method] `zoomOut()`
  - [method] `verifierSignature()`
  - [method] `fermerModal()`

### BesoinFormComponent (class)
- Fichier: `Frontend\src\app\modules\besoins\besoin-form\besoin-form.component.ts`
- Ligne de d?claration approx.: 20
- M?thodes:
  - [constructor] `constructor(private fb: FormBuilder,
    private besoinsService: BesoinsService,
    private categoriesService: CategoriesService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `submit()`

### BesoinsListComponent (class)
- Fichier: `Frontend\src\app\modules\besoins\besoins-list\besoins-list.component.ts`
- Ligne de d?claration approx.: 21
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private besoinsService: BesoinsService,
    private categoriesService: CategoriesService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `onFiltreStatutChange(val: string | null)`
  - [method] `reinitialiserFiltre()`
  - [method] `ngOnInit()`
  - [method] `load()`
  - [method] `openCreate()`
  - [method] `fermerModal()`
  - [method] `onFichierChange(event: Event)`
  - [method] `supprimerFichierSelectionne()`
  - [method] `openEdit(b: BesoinDTO)`
  - [method] `fermerEditModal()`
  - [method] `onFichierEditChange(event: Event)`
  - [method] `submitEdit()`
  - [method] `submit()`
  - [method] `ouvrirCircuit(b: BesoinDTO)`
  - [method] `fermerCircuit()`
  - [method] `delete(id: number)`

### CategoriesComponent (class)
- Fichier: `Frontend\src\app\modules\categories\categories.component.ts`
- Ligne de d?claration approx.: 20
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private categoriesService: CategoriesService,
    private workflowService: WorkflowService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `load()`
  - [method] `openCreate()`
  - [method] `openEdit(c: CategorieDTO)`
  - [method] `submit()`
  - [method] `delete(id: number)`

### DashboardComponent (class)
- Fichier: `Frontend\src\app\modules\dashboard\dashboard.component.ts`
- Ligne de d?claration approx.: 17
- M?thodes:
  - [constructor] `constructor(private reportingService: ReportingService)`
  - [getter] `get totalRecus()`
  - [getter] `get totalApprouves()`
  - [getter] `get totalRejetes()`
  - [getter] `get maxAttente()`
  - [method] `ngOnInit()`
  - [method] `ngAfterViewInit()`
  - [method] `ngOnDestroy()`
  - [method] `chargerEvolution()`
  - [method] `changerPeriode(p: '' | '' | '')`
  - [method] `renderChart()`

### LogsComponent (class)
- Fichier: `Frontend\src\app\modules\logs\logs.component.ts`
- Ligne de d?claration approx.: 12
- M?thodes:
  - [constructor] `constructor(private logsService: LogsService)`
  - [method] `ngOnInit()`
  - [method] `load()`
  - [method] `nextPage()`
  - [method] `prevPage()`

### MaSignatureComponent (class)
- Fichier: `Frontend\src\app\modules\ma-signature\ma-signature.component.ts`
- Ligne de d?claration approx.: 15
- M?thodes:
  - [constructor] `constructor(private service: MaSignatureService,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `initCanvas()`
  - [method] `onMouseDown(e: MouseEvent)`
  - [method] `onMouseMove(e: MouseEvent)`
  - [method] `onMouseUp()`
  - [method] `onTouchStart(e: TouchEvent)`
  - [method] `onTouchMove(e: TouchEvent)`
  - [method] `effacer()`
  - [method] `onFileChange(event: Event)`
  - [method] `sauvegarder()`
  - [method] `supprimer()`
  - [method] `setOnglet(o: '' | '' | '')`

### NotificationsComponent (class)
- Fichier: `Frontend\src\app\modules\notifications\notifications.component.ts`
- Ligne de d?claration approx.: 20
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private notifService: NotificationsService,
    private usersService: UsersService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `onFiltreLectureChange(val: string | null)`
  - [method] `reinitialiserFiltre()`
  - [method] `toggleDestinataire(id: number, event: Event)`
  - [method] `toggleTous(event: Event)`
  - [method] `ngOnInit()`
  - [method] `marquerLu(id: number)`
  - [method] `envoyer()`
  - [method] `fermerForm()`

### ProfilComponent (class)
- Fichier: `Frontend\src\app\modules\profil\profil.component.ts`
- Ligne de d?claration approx.: 22
- M?thodes:
  - [constructor] `constructor(private auth: AuthService,
    private usersService: UsersService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [method] `ngOnInit()`
  - [method] `changerMotDePasse()`

### ReportingComponent (class)
- Fichier: `Frontend\src\app\modules\reporting\reporting.component.ts`
- Ligne de d?claration approx.: 28
- M?thodes:
  - [constructor] `constructor(public auth: AuthService, private reportingService: ReportingService, private rolesService: RolesService, private fb: FormBuilder)`
  - [method] `ngOnInit()`
  - [method] `rechercher()`
  - [method] `exporter(format: '' | '')`

### WorkflowComponent (class)
- Fichier: `Frontend\src\app\modules\workflow\workflow.component.ts`
- Ligne de d?claration approx.: 20
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    private workflowService: WorkflowService,
    private rolesService: RolesService,
    private fb: FormBuilder,
    private toastr: ToastrService)`
  - [getter] `get etapes()`
  - [method] `ngOnInit()`
  - [method] `load()`
  - [method] `openCreate()`
  - [method] `openEdit(c: WorkflowCircuitDTO)`
  - [method] `addEtape()`
  - [method] `removeEtape(i: number)`
  - [method] `submit()`
  - [method] `delete(id: number)`
  - [method] `select(c: WorkflowCircuitDTO)`

### CustomSelectComponent (class)
- Fichier: `Frontend\src\app\shared\components\custom-select\custom-select.component.ts`
- Ligne de d?claration approx.: 19
- M?thodes:
  - [method] `toggle()`
  - [method] `openDropdown()`
  - [method] `closeDropdown()`
  - [method] `createDropdownPortal()`
  - [method] `removeDropdownPortal()`
  - [method] `reposition()`
  - [method] `closeOnOutsideClick(event: Event)`
  - [method] `ngOnDestroy()`

### ForbiddenComponent (class)
- Fichier: `Frontend\src\app\shared\components\forbidden\forbidden.component.ts`
- Ligne de d?claration approx.: 10
- M?thodes: aucune m?thode d?tect?e

### LayoutComponent (class)
- Fichier: `Frontend\src\app\shared\components\layout\layout.component.ts`
- Ligne de d?claration approx.: 16
- M?thodes:
  - [constructor] `constructor(public auth: AuthService,
    public theme: ThemeService,
    private notifService: NotificationsService,
    private router: Router)`
  - [method] `confirmLogout()`
  - [method] `cancelLogout()`
  - [method] `doLogout()`
  - [method] `toggle()`
  - [method] `ngOnInit()`
  - [method] `chargerNotifications()`
  - [method] `toggleNotifDropdown(event: Event)`
  - [method] `marquerLuEtNaviguer(notif: NotificationDTO)`
  - [method] `fermerDropdown()`

### NiveauOptionsPipe (class)
- Fichier: `Frontend\src\app\shared\pipes\select-options.pipe.ts`
- Ligne de d?claration approx.: 6
- M?thodes: aucune m?thode d?tect?e

### CategorieOptionsPipe (class)
- Fichier: `Frontend\src\app\shared\pipes\select-options.pipe.ts`
- Ligne de d?claration approx.: 14
- M?thodes: aucune m?thode d?tect?e

### RoleOptionsPipe (class)
- Fichier: `Frontend\src\app\shared\pipes\select-options.pipe.ts`
- Ligne de d?claration approx.: 22
- M?thodes: aucune m?thode d?tect?e

### CircuitOptionsPipe (class)
- Fichier: `Frontend\src\app\shared\pipes\select-options.pipe.ts`
- Ligne de d?claration approx.: 32
- M?thodes: aucune m?thode d?tect?e

