﻿using ERwin_CA.T;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VBClassLibrary;

namespace ERwin_CA
{
    class ConnMng
    {
        public SCAPI.Application scERwin;
        public SCAPI.PersistenceUnit scPersistenceUnit;
        public SCAPI.Session scSession;
        public SCAPI.ModelObject erRootObj { get; set; }
        public SCAPI.ModelObjects erRootObjCol { get; set; }
        public SCAPI.ModelObjects erColumn { get; set; }
        public SCAPI.ModelObjects erTable { get; set; }
        public SCAPI.ModelObject scItem;
        public SCAPI.ModelObject scDB;
        public SCAPI.ModelObject scSchema;
        public string fileERwin = null;
        public FileInfo fileInfoERwin = null;
        public List<string> DatabaseN = null;
        public List<string> SchemaN = null;
        public object trID { get; set; }

        public bool openModelConnection(string ERw)
        {
            if (ERw == null)
                return false;
            if (!File.Exists(ERw))
            {
                Logger.PrintLC("Could not find file: " + ERw);
                return false;
            }
            
            try
            {
                DatabaseN = new List<string>();
                SchemaN = new List<string> { "SYSADM", "SYSFUN", "SYSIBM", "SYSPROC", "SYSTOOLS" };
                fileERwin = ERw;
                fileInfoERwin = new FileInfo(fileERwin);
                scERwin = new SCAPI.Application();

                FileOps.RemoveAttributes(ERw);
                scPersistenceUnit = scERwin.PersistenceUnits.Add(ERw, "RDO=No");

                scSession = scERwin.Sessions.Add();
                scSession.Open(scPersistenceUnit);
                Logger.PrintLC("Connection opened.");
                return true;
            }
            catch (Exception exp)
            {
                Logger.PrintLC("Connection opening error: " + exp.Message);
                return false;
            }
        }

        public void CloseModelConnection()
        {
            try
            {
                scERwin.PersistenceUnits.Clear();
                SCAPI.Sessions scSessionCol = scERwin.Sessions;
                foreach (SCAPI.Session scSes in scSessionCol)
                    scSes.Close();

                while (scSessionCol.Count > 0)
                    scSessionCol.Remove(0);

                scPersistenceUnit = null;
                //scSession.Close();
                Logger.PrintLC("Session closed successfully.");
            }
            catch (Exception exp)
            {
                Logger.PrintLC("Could not close the Session.");
            }
        }



        public object OpenTransaction()
        {
            if (scSession != null)
                try
                {
                    trID = scSession.BeginTransaction();
                    Logger.PrintLC("Transaction began successfully.");
                    return trID;
                }
                catch (Exception exp)
                {
                    Logger.PrintLC("Starting Transaction error: " + exp.Message);
                    return -1;
                }
            else
                Logger.PrintLC("Starting Transaction error: missing SESSION.");
            return -1;
        }

        public bool SetRootObject()
        {
            if (scSession != null)
                try
                {
                    erRootObj = scSession.ModelObjects.Root;
                    Logger.PrintLC("Root has been successful.");
                    return true;
                }
                catch (Exception exp)
                {
                    Logger.PrintLC("Setting Root's Session error: " + exp.Message);
                    return false;
                }
            else
                Logger.PrintLC("Could not determine Root because Session is missing.");
            return false;
        }

        public bool SetRootCollection()
        {
            if (scSession != null)
                try
                {
                    erRootObjCol = scSession.ModelObjects.Collect(erRootObj);
                    Logger.PrintLC("Root Collection has been successful.");
                    return true;
                }
                catch (Exception exp)
                {
                    Logger.PrintLC("Could not get the Root Collection: " + exp.Message);
                    return false;
                }
            else
            {
                Logger.PrintLC("Could not get Root Collection because Session is missing.");
                return false;
            }
        }

        public SCAPI.ModelObject CreateEntity (EntityT entity, string db)
        {
            SCAPI.ModelObject ret = null;
            if (string.IsNullOrWhiteSpace(db))
            {
                Logger.PrintLC("There was no DB associated to " + entity.TableName);
                return ret;
            }
            if (!((entity.FlagBFD == "S") || (entity.FlagBFD == "N")))
            {
                Logger.PrintLC("Property FlagBFD of " + entity.TableName + " is not valid. Table will be skipped.");
                return ret;
            }

            if (erRootObjCol != null)
            {
                OpenTransaction();
                scItem = erRootObjCol.Add("Entity");
                VBCon con = new VBCon();

                //Controlli proprietà essenziali
                //Nome tabella
                if (!string.IsNullOrWhiteSpace(entity.TableName))
                {
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Nome Tabella"], entity.TableName))
                        Logger.PrintLC("Added Table Physical Name to " + scItem.ObjectId);
                    else
                    {
                        Logger.PrintLC("Error adding Table Physical Name to " + scItem.ObjectId);
                        return scItem;
                    }
                    if (con.AssignToObjModel(ref scItem, "Name", entity.TableName))
                        Logger.PrintLC("Added Table Name to " + scItem.ObjectId);
                    else
                    {
                        Logger.PrintLC("Error adding Table Name to " + scItem.ObjectId);
                        return scItem;
                    }
                }

                //SSA
                if (!string.IsNullOrWhiteSpace(entity.SSA))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["SSA"], entity.SSA))
                        Logger.PrintLC("Added SSA to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding SSA to " + scItem.Name);
                
                //Table Description
                if (!string.IsNullOrWhiteSpace(entity.TableDescr))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Descrizione Tabella"], entity.TableDescr))
                        Logger.PrintLC("Added Table Description to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Table Description to " + scItem.Name);
                if (!string.IsNullOrWhiteSpace(entity.TableDescr))
                    if (con.AssignToObjModel(ref scItem, "Definition", entity.TableDescr))
                        Logger.PrintLC("Added Table Definition to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Table Definition to " + scItem.Name);


                //Info Type
                if (!string.IsNullOrWhiteSpace(entity.InfoType))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Tipologia Informazione"], entity.InfoType))
                        Logger.PrintLC("Added Information Type to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Information Type to " + scItem.Name);

                //Table Limit
                if (!string.IsNullOrWhiteSpace(entity.TableLimit))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Perimetro Tabella"], entity.TableLimit))
                        Logger.PrintLC("Added Table Limit to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Table Limit to " + scItem.Name);

                //Table Granularity
                if (!string.IsNullOrWhiteSpace(entity.TableGranularity))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Granularità Tabella"], entity.TableGranularity))
                        Logger.PrintLC("Added Table Granularity to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Table Granularity to " + scItem.Name);

                //Flag BFD
                if (!string.IsNullOrWhiteSpace(entity.FlagBFD))
                    if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Flag BFD"], entity.FlagBFD))
                        Logger.PrintLC("Added Flag BFD to " + scItem.Name);
                    else
                        Logger.PrintLC("Error adding Flag BFD to " + scItem.Name);


                //FileOps.RemoveAttributes(fileERwin);
                //scSession.CommitTransaction(trID);
                //scPersistenceUnit.Save();
                //scSession.Close();

                //##################################################
                //## Controllo esistenza DB ed eventuale aggiunta ##
                if (!string.IsNullOrWhiteSpace(entity.DatabaseName))
                    if (!DatabaseN.Contains(entity.DatabaseName))
                    {
                        scDB = erRootObjCol.Add("DB2_Database");
                        if (con.AssignToObjModel(ref scDB, ConfigFile._TAB_NAME["Nome Database"], entity.DatabaseName))
                            Logger.PrintLC("Added Database Name to " + scDB.Name);
                        else
                            Logger.PrintLC("Error adding Database Name to " + scDB.Name);

                        if (!string.IsNullOrWhiteSpace(entity.HostName))
                            if (con.AssignToObjModel(ref scDB, ConfigFile._TAB_NAME["Nome host"], entity.HostName))
                                Logger.PrintLC("Added Host Name to " + scDB.Name);
                            else
                                Logger.PrintLC("Error adding Host Name to " + scDB.Name);
                        DatabaseN.Add(entity.DatabaseName);
                    }
                //##################################################
                //##################################################
                //## Controllo esistenza SCHEMA ed eventuale aggiunta ##
                if (!string.IsNullOrWhiteSpace(entity.Schema))
                {
                    if (!SchemaN.Contains(entity.Schema))
                    {
                        scSchema = erRootObjCol.Add("Schema");
                        if (con.AssignToObjModel(ref scSchema, "Name", entity.Schema))
                            Logger.PrintLC("Created Schema Name to " + scSchema.Name);
                        else
                            Logger.PrintLC("Error creating Schema Name to " + scSchema.Name);
                        SchemaN.Add(entity.Schema);
                    }
                    //Schema
                    if (!string.IsNullOrWhiteSpace(entity.Schema))
                        if (con.AssignToObjModel(ref scItem, ConfigFile._TAB_NAME["Schema"], entity.Schema))
                            Logger.PrintLC("Added Schema to " + scItem.Name);
                        else
                            Logger.PrintLC("Error adding Schema to " + scItem.Name);
                }
                //##################################################

                CommitAndSave(trID);
            }
            return scItem;
        }


        public bool CommitAndSave(object id)
        {
            if (!CommitTransaction(id))
                return false;
            if (!SavePersistence())
                return false;
            return true;
        }

        public bool CommitTransaction(object id)
        {
            try
            {
                if (!scSession.CommitTransaction(id))
                {
                    Logger.PrintLC("Could not Commit for ID: " + id);
                    return false;
                }
                else
                {
                    Logger.PrintLC("Committed successfully: " + id);
                    return true;
                }
            }
            catch (Exception exp)
            {
                Logger.PrintLC("Could NOT Commit.");
                return false;
            }
        }

        public bool SavePersistence()
        {
            try
            {
                FileOps.RemoveAttributes(fileERwin);
                if (!scPersistenceUnit.Save())
                {
                    Logger.PrintLC("Could NOT save Persistence: " + scPersistenceUnit.ObjectId);
                    return false;
                }
                else
                {
                    Logger.PrintLC("Persistence SAVED: " + scPersistenceUnit.ObjectId);
                    return true;
                }
            }
            catch (Exception exp)
            {
                Logger.PrintLC("Persistence NOT saved.");
                return false;
            }
        }

        public static bool AssignToObjModel(ref SCAPI.ModelObject model, string property, string value)
        {
            VBCon VBcon = new VBCon();
            if (VBcon.AssignToObjModel(ref model, property, value))
                return true;
            else
                return false;
        }

        public static bool AssignToObjModel(ref SCAPI.ModelObject model, string property, int value)
        {
            VBCon VBcon = new VBCon();
            if (VBcon.AssignToObjModel(ref model, property, value))
                return true;
            else
                return false;
        }
    }
}
