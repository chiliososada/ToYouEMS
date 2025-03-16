/*
 Navicat MySQL Dump SQL

 Source Server         : ToYouEmsDB
 Source Server Type    : MySQL
 Source Server Version : 80400 (8.4.0)
 Source Host           : 127.0.0.1:3306
 Source Schema         : toyouems

 Target Server Type    : MySQL
 Target Server Version : 80400 (8.4.0)
 File Encoding         : 65001

 Date: 14/03/2025 16:42:10
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for attendance
-- ----------------------------
DROP TABLE IF EXISTS `attendance`;
CREATE TABLE `attendance`  (
  `AttendanceID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `Month` varchar(7) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `FileUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `UploadDate` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `Status` int NOT NULL DEFAULT 0 COMMENT '-- 映射关系: 0=Pending, 1=Approved, 2=Rejected\n',
  PRIMARY KEY (`AttendanceID`) USING BTREE,
  INDEX `UserID`(`UserID` ASC) USING BTREE,
  CONSTRAINT `attendance_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for cases
-- ----------------------------
DROP TABLE IF EXISTS `cases`;
CREATE TABLE `cases`  (
  `CaseID` int NOT NULL AUTO_INCREMENT,
  `CaseName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CompanyName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Position` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `InterviewDate` date NULL DEFAULT NULL,
  `Location` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `ContactPerson` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `ContactInfo` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Status` int NOT NULL DEFAULT 0 COMMENT '-- 映射关系: 0=Active, 1=Completed, 2=Cancelled',
  `CreatedBy` int NOT NULL,
  `CreatedAt` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`CaseID`) USING BTREE,
  INDEX `CreatedBy`(`CreatedBy` ASC) USING BTREE,
  CONSTRAINT `cases_ibfk_1` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for logs
-- ----------------------------
DROP TABLE IF EXISTS `logs`;
CREATE TABLE `logs`  (
  `LogID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NULL DEFAULT NULL,
  `Action` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `LogTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`LogID`) USING BTREE,
  INDEX `UserID`(`UserID` ASC) USING BTREE,
  CONSTRAINT `logs_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 16 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for profiles
-- ----------------------------
DROP TABLE IF EXISTS `profiles`;
CREATE TABLE `profiles`  (
  `ProfileID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `FullName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Gender` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `BirthDate` date NULL DEFAULT NULL,
  `BirthPlace` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Address` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `Introduction` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Hobbies` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `AvatarUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  PRIMARY KEY (`ProfileID`) USING BTREE,
  INDEX `UserID`(`UserID` ASC) USING BTREE,
  CONSTRAINT `profiles_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 5 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for questionrevisions
-- ----------------------------
DROP TABLE IF EXISTS `questionrevisions`;
CREATE TABLE `questionrevisions`  (
  `RevisionID` int NOT NULL AUTO_INCREMENT,
  `QuestionID` int NOT NULL,
  `UserID` int NOT NULL,
  `RevisionText` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Type` int NOT NULL COMMENT '-- 0=Answer, 1=TeacherEdit, 2=TeacherComment',
  `Comments` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `CreatedAt` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`RevisionID`) USING BTREE,
  INDEX `idx_questionrevisions_questionid`(`QuestionID` ASC) USING BTREE,
  INDEX `idx_questionrevisions_userid`(`UserID` ASC) USING BTREE,
  INDEX `idx_questionrevisions_createdat`(`CreatedAt` ASC) USING BTREE,
  CONSTRAINT `questionrevisions_ibfk_1` FOREIGN KEY (`QuestionID`) REFERENCES `questions` (`QuestionID`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `questionrevisions_ibfk_2` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for questions
-- ----------------------------
DROP TABLE IF EXISTS `questions`;
CREATE TABLE `questions`  (
  `QuestionID` int NOT NULL AUTO_INCREMENT,
  `CaseID` int NOT NULL,
  `UserID` int NOT NULL,
  `QuestionText` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Answer` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Source` int NOT NULL DEFAULT 0 COMMENT '-- 映射关系: 0=Personal, 1=Company',
  `Status` int NOT NULL DEFAULT 0 COMMENT '-- 映射关系: 0=Pending, 1=Approved, 2=Rejected',
  `CreatedAt` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`QuestionID`) USING BTREE,
  INDEX `CaseID`(`CaseID` ASC) USING BTREE,
  INDEX `UserID`(`UserID` ASC) USING BTREE,
  CONSTRAINT `questions_ibfk_1` FOREIGN KEY (`CaseID`) REFERENCES `cases` (`CaseID`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `questions_ibfk_2` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for resumes
-- ----------------------------
DROP TABLE IF EXISTS `resumes`;
CREATE TABLE `resumes`  (
  `ResumeID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `FileName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `FileUrl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `UploadDate` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  `Status` int NOT NULL DEFAULT 0 COMMENT ' Pending,Approved, Rejected',
  `Comments` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `ReviewerID` int NULL DEFAULT NULL,
  PRIMARY KEY (`ResumeID`) USING BTREE,
  INDEX `UserID`(`UserID` ASC) USING BTREE,
  INDEX `ReviewerID`(`ReviewerID` ASC) USING BTREE,
  CONSTRAINT `resumes_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `resumes_ibfk_2` FOREIGN KEY (`ReviewerID`) REFERENCES `users` (`UserID`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for stats
-- ----------------------------
DROP TABLE IF EXISTS `stats`;
CREATE TABLE `stats`  (
  `StatID` int NOT NULL AUTO_INCREMENT,
  `StatCategory` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `StatName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `StatValue` int NOT NULL,
  `LastUpdated` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`StatID`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `UserID` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
  `UserType` int NOT NULL COMMENT ' Student = 0,Teacher = 1,Admin = 2',
  `IsActive` tinyint(1) NULL DEFAULT 1,
  `CreatedAt` datetime NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`UserID`) USING BTREE,
  UNIQUE INDEX `Username`(`Username` ASC) USING BTREE,
  UNIQUE INDEX `Email`(`Email` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 5 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- View structure for vw_questions_with_revisions
-- ----------------------------
DROP VIEW IF EXISTS `vw_questions_with_revisions`;
CREATE ALGORITHM = UNDEFINED SQL SECURITY DEFINER VIEW `vw_questions_with_revisions` AS select `q`.`QuestionID` AS `QuestionID`,`q`.`CaseID` AS `CaseID`,`c`.`CaseName` AS `CaseName`,`c`.`CompanyName` AS `CompanyName`,`q`.`UserID` AS `UserID`,`u`.`Username` AS `Username`,`q`.`QuestionText` AS `QuestionText`,`q`.`Answer` AS `Answer`,`q`.`Source` AS `Source`,`q`.`Status` AS `Status`,`q`.`CreatedAt` AS `CreatedAt`,count(`r`.`RevisionID`) AS `RevisionCount` from (((`questions` `q` join `users` `u` on((`q`.`UserID` = `u`.`UserID`))) join `cases` `c` on((`q`.`CaseID` = `c`.`CaseID`))) left join `questionrevisions` `r` on((`q`.`QuestionID` = `r`.`QuestionID`))) group by `q`.`QuestionID`;

SET FOREIGN_KEY_CHECKS = 1;
