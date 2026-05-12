-- Script SQL pour créer le rôle Administrateur dans FinAssist
-- À exécuter une seule fois avant les tests de performance

USE FinAssistDB;
GO

-- Vérifier si le rôle existe déjà
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = 'Administrateur')
BEGIN
    INSERT INTO Roles (Code, Description, DateCreation, DateModification)
    VALUES ('Administrateur', 'Administrateur système avec tous les droits', GETUTCDATE(), GETUTCDATE());
    
    PRINT 'Rôle Administrateur créé avec succès.';
END
ELSE
BEGIN
    PRINT 'Le rôle Administrateur existe déjà.';
END
GO

-- Afficher l'ID du rôle Administrateur
SELECT Id, Code, Description FROM Roles WHERE Code = 'Administrateur';
GO
