CREATE DATABASE  IF NOT EXISTS `rtsmultiplayergame` /*!40100 DEFAULT CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `rtsmultiplayergame`;
-- MySQL dump 10.13  Distrib 8.0.31, for Win64 (x86_64)
--
-- Host: localhost    Database: rtsmultiplayergame
-- ------------------------------------------------------
-- Server version	8.0.31

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `allocated_server`
--

DROP TABLE IF EXISTS `allocated_server`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `allocated_server` (
  `server_id` bigint NOT NULL,
  `ip` varchar(40) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `port` smallint unsigned NOT NULL,
  `password` varchar(65) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  PRIMARY KEY (`server_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `allocated_server`
--

LOCK TABLES `allocated_server` WRITE;
/*!40000 ALTER TABLE `allocated_server` DISABLE KEYS */;
/*!40000 ALTER TABLE `allocated_server` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `owned_combat_unit`
--

DROP TABLE IF EXISTS `owned_combat_unit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `owned_combat_unit` (
  `user_id` bigint unsigned NOT NULL,
  `combat_unit_id` int unsigned NOT NULL,
  `unit_level` int NOT NULL,
  `position_in_deck` tinyint unsigned DEFAULT NULL,
  PRIMARY KEY (`user_id`,`combat_unit_id`),
  CONSTRAINT `owned_combat_unit_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `owned_combat_unit`
--

LOCK TABLES `owned_combat_unit` WRITE;
/*!40000 ALTER TABLE `owned_combat_unit` DISABLE KEYS */;
INSERT INTO `owned_combat_unit` VALUES (5,1,10,NULL),(5,2,10,3),(5,3,1,7),(5,4,1,8),(5,5,1,5),(5,6,1,1),(5,7,1,NULL),(5,8,1,4),(5,9,1,NULL),(5,10,1,6),(5,11,1,2),(5,12,1,NULL),(6,1,10,4),(6,2,10,1),(6,3,1,3),(6,4,1,2),(6,5,1,8),(6,6,1,5),(6,7,1,7),(6,8,1,6),(10,1,1,5),(10,2,1,1),(10,3,1,3),(10,4,1,8),(10,5,1,2),(10,6,1,6),(10,7,1,7),(10,8,1,4),(11,1,2,5),(11,2,1,2),(11,3,1,7),(11,4,1,4),(11,5,1,1),(11,6,1,6),(11,7,1,3),(11,8,1,8);
/*!40000 ALTER TABLE `owned_combat_unit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `player_data`
--

DROP TABLE IF EXISTS `player_data`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `player_data` (
  `user_id` bigint unsigned NOT NULL,
  `gold` bigint unsigned NOT NULL,
  `trophies` int NOT NULL,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `player_data_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `player_data`
--

LOCK TABLES `player_data` WRITE;
/*!40000 ALTER TABLE `player_data` DISABLE KEYS */;
INSERT INTO `player_data` VALUES (5,678896,270),(6,704903,60),(10,999999,1234),(11,2000,0);
/*!40000 ALTER TABLE `player_data` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user` (
  `user_id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `username` varchar(30) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `password_hash` varchar(255) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `email` varchar(320) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `player_name` varchar(30) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `registration_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb3;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user`
--

LOCK TABLES `user` WRITE;
/*!40000 ALTER TABLE `user` DISABLE KEYS */;
INSERT INTO `user` VALUES (5,'ioan45','$2y$10$cvNhAgTsUJSHpQnEBYoZ1egmnBH.T0.GTTgtMU.xrAgCuC1YfSkRS','ioan45@gmail.com','ioan45Player','2023-04-16 03:30:56'),(6,'ioan46','$2y$10$Ld.aAYDRadlTmOF45BmBsOh2u3.jZ7XlPZbtTB3ZzsnrcFxoQPD3u','ioan46@yahoo.com','ioan46OtherPlayer','2023-04-16 20:06:59'),(10,'ioan99','$2y$10$lmiN0jqs3BFW9CKJk16evOOIrBVGteO3kY.R/oN9YkhYD9uy3k9XO','ioan99@gmail.com','ioan99Papucesti','2023-05-19 01:27:53'),(11,'ioan101','$2y$10$YuTT4Dm3Sajx3Ocw0Jy6G.d8oOaMKXiB/0oy3a67xoN1uKsU9ffBK','ioan101@gmail.com','ioan1012','2023-05-20 13:06:46');
/*!40000 ALTER TABLE `user` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_in_match`
--

DROP TABLE IF EXISTS `user_in_match`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_in_match` (
  `user_id` bigint unsigned NOT NULL,
  `server_id` bigint NOT NULL,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `user_in_match_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_in_match`
--

LOCK TABLES `user_in_match` WRITE;
/*!40000 ALTER TABLE `user_in_match` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_in_match` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_login_session`
--

DROP TABLE IF EXISTS `user_login_session`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_login_session` (
  `user_id` bigint unsigned NOT NULL,
  `session_token` varchar(64) CHARACTER SET utf8mb3 COLLATE utf8mb3_unicode_ci NOT NULL,
  `session_start_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `user_login_session_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_login_session`
--

LOCK TABLES `user_login_session` WRITE;
/*!40000 ALTER TABLE `user_login_session` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_login_session` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'rtsmultiplayergame'
--

--
-- Dumping routines for database 'rtsmultiplayergame'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2023-07-02 18:25:52
