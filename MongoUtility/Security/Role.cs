﻿using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoUtility.Security
{
    public class Role
    {
        #region"用户角色"

        public const String UserRole_read = "read";
        public const String UserRole_readWrite = "readWrite";
        public const String UserRole_dbAdmin = "dbAdmin";
        public const String UserRole_userAdmin = "userAdmin";
        public const String UserRole_clusterAdmin = "clusterAdmin";
        public const String UserRole_readAnyDatabase = "readAnyDatabase";
        public const String UserRole_readWriteAnyDatabase = "readWriteAnyDatabase";
        public const String UserRole_userAdminAnyDatabase = "userAdminAnyDatabase";
        public const String UserRole_dbAdminAnyDatabase = "dbAdminAnyDatabase";

        #endregion

        #region"角色操作"

        /// <summary>
        ///     可以省略？？
        /// </summary>
        public String _id;

        /// <summary>
        ///     rolename
        /// </summary>
        public String rolename;

        /// <summary>
        /// </summary>
        public String database;

        /// <summary>
        /// </summary>
        public privilege[] privileges;

        /// <summary>
        /// </summary>
        public GrantRole[] roles;

        /// <summary>
        ///     权限
        /// </summary>
        public struct privilege
        {
            /// <summary>
            /// </summary>
            public MongoDBAction.ActionType[] actions;

            /// <summary>
            /// </summary>
            public Resource resource;
        }

        /// <summary>
        ///     角色
        /// </summary>
        public struct GrantRole
        {
            /// <summary>
            /// </summary>
            public String db;

            /// <summary>
            /// </summary>
            public String mRole;
        }

        /// <summary>
        ///     添加一个用户自定义角色
        /// </summary>
        public static BsonValue AddRole(MongoDatabase mongoDb, Role role)
        {
            var RoleCommand = String.Empty;
            var doc = new EvalArgs();
            RoleCommand = "db.createRole(" + Environment.NewLine;
            RoleCommand += "{" + Environment.NewLine;
            RoleCommand += "    role: '" + role.rolename + "'," + Environment.NewLine;
            //Roles
            RoleCommand += "    roles:" + Environment.NewLine;
            RoleCommand += "    [";
            for (var i = 0; i < role.roles.Length; i++)
            {
                var singleroles = role.roles[i];
                RoleCommand += "{ role: '" + singleroles.mRole + "', db: '" + singleroles.db + "' }" +
                               ((i == role.roles.Length - 1) ? "" : ",") + Environment.NewLine;
            }
            RoleCommand += "     ],";
            //privileges
            RoleCommand += "    privileges:" + Environment.NewLine;
            RoleCommand += "    [";
            for (var i = 0; i < role.privileges.Length; i++)
            {
                var singleprivileges = role.privileges[i];
                RoleCommand += "{" + singleprivileges.resource.GetJsCode() + "," +
                               MongoDBAction.GetActionListJs(singleprivileges.actions) + "}" +
                               ((i == role.privileges.Length - 1) ? "" : ",") + Environment.NewLine;
            }
            RoleCommand += "     ],";
            //
            RoleCommand += "}" + Environment.NewLine;
            RoleCommand += ")";
            doc.Code = RoleCommand;
            BsonValue result;
            try
            {
                result = mongoDb.Eval(doc);
            }
            catch (MongoCommandException ex)
            {
                result = ex.Result;
            }
            return result;
        }

        /// <summary>
        ///     GetRole
        /// </summary>
        /// <param name="mongoDb"></param>
        /// <param name="RoleName"></param>
        /// <returns></returns>
        public static BsonDocument GetRole(MongoDatabase mongoDb, String RoleName)
        {
            var doc = new EvalArgs();
            doc.Code = "db.getRole('" + RoleName + "',{showPrivileges:true})";
            return mongoDb.Eval(doc).AsBsonDocument;
        }

        #endregion
    }
}