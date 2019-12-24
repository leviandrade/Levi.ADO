using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace FatecProjeto.Ado
{
    public class ADO
    {
        private readonly string connection;
        public ADO(string connection)
        {
            this.connection = connection;
        }

        public List<T> List<T>()
        {
            var list = new List<T>();

            var data = typeof(T);
            var properties = typeof(T).GetProperties();

            var query = "Select * from " + data.Name + " where excluido = 0";

            using (var conn = new SqlConnection(connection))
            {
                var cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            var entity = Activator.CreateInstance(data);
                            foreach (var propertie in properties)
                            {
                                var type = Type.GetTypeCode(propertie.PropertyType);

                                try
                                {
                                    var value = Convert.ChangeType(reader[propertie.Name], type);

                                    if (value == DBNull.Value)
                                        value = null;

                                    propertie.SetValue(entity, value, null);
                                }
                                catch { }
                            }

                            list.Add((T)entity);
                        }
                    }
                }

                catch (Exception e)
                {
                    conn.Close();
                    throw e;
                }
            }
            return list;
        }

        public List<T> Consult<T>(string consult)
        {
            return Consult<T>(consult, new Dictionary<string, object>());
        }

        public List<T> Consult<T>(string consult, Dictionary<string, object> parameters)
        {
            var list = new List<T>();

            var data = typeof(T);

            var properties = data.GetProperties();

            using (var conn = new SqlConnection(connection))
            {
                try
                {
                    var cmd = new SqlCommand(consult, conn);
                    foreach (var item in parameters)
                        cmd.Parameters.AddWithValue(item.Key, item.Value);
                    
                    conn.Open();
                    using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            var entity = Activator.CreateInstance(data);
                            foreach (var propertie in properties)
                            {
                                if (propertie.SetMethod == null)
                                    continue;

                                try
                                {
                                    var value = Convert.ChangeType(reader[propertie.Name], Type.GetTypeCode(propertie.PropertyType));

                                    if (value == DBNull.Value)
                                        value = null;

                                    propertie.SetValue(entity, value, null);
                                }
                                catch { }
                            }

                            list.Add((T)entity);
                        }
                    }
                }

                catch (Exception e)
                {
                    conn.Close();
                    throw e;
                }
            }

            return list;
        }

        public int Save<T>(T entity)
        {
            return Save(entity, null);
        }

        public int Save<T>(T entity, int? id)
        {
            var idSaved = id.HasValue ? id.Value : 0;

            var data = entity.GetType();
            var properties = data.GetProperties().Where(x => x.Name != "id").ToArray();
            var columns = new List<string>();

            var query = "";

            using (var conn = new SqlConnection(connection))
            {
                if (id.HasValue && id.Value > 0)
                {
                    query = "Update " + data.Name + " Set ";
                    for (var i = 0; i < properties.Count(); i++)
                    {
                        query += properties[i].Name + " = @" + properties[i].Name;
                        if (properties.Count() - i > 1)
                            query += ",";
                    }
                    query += " where id= " + id + ";SELECT SCOPE_IDENTITY()";
                }
                else
                {
                    for (var i = 0; i < properties.Count(); i++)
                        columns.Add("@" + properties[i].Name);

                    query = "INSERT INTO " + data.Name + " (" + string.Join(",", columns.Select(x => x.Replace("@", "")).ToList()) 
                        + ") VALUES (" + string.Join(",", columns) + ");SELECT SCOPE_IDENTITY()";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                for (var i = 0; i < properties.Count(); i++)
                    cmd.Parameters.AddWithValue(("@" + properties[i].Name), properties[i].GetValue(entity) ?? DBNull.Value);

                try
                {
                    conn.Open();

                    if (idSaved > 0)
                        cmd.ExecuteNonQuery();
                    else
                        idSaved = Convert.ToInt32(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    conn.Close();
                    throw e;
                }
            }
            return idSaved;
        }

        public int Delete<T>(int id)
        {
            var entity = typeof(T);
            using (var conn = new SqlConnection(connection))
            {
                string query = "update " + entity.Name + " set excluido = 1 where id=" + id;
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    conn.Close();
                    throw e;
                }
            }

            return id;
        }


        public T Select<T>(int id)
        {
            var list = new List<T>();

            var data = typeof(T);

            using (var conn = new SqlConnection(connection))
            {

                var consulta = "select * from " + data.Name + " where id = " + id;
                var cmd = new SqlCommand(consulta, conn);

                PropertyInfo[] properties = data.GetProperties();

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            var entity = Activator.CreateInstance(data);
                            foreach (var item in properties)
                            {
                                var type = Type.GetTypeCode(item.PropertyType);

                                try
                                {
                                    var valor = Convert.ChangeType(reader[item.Name], type);

                                    if (valor == DBNull.Value)
                                        valor = null;

                                    item.SetValue(entity, valor, null);
                                }
                                catch { }
                            }

                            list.Add((T)entity);
                        }
                    }
                }

                catch (Exception e)
                {
                    conn.Close();
                    throw e;
                }
            }

            return list.FirstOrDefault();
        }
    }
}