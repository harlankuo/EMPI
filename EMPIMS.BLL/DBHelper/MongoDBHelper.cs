using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Data;
using EMPIMS.Code;
using EMPIMS.Code.Security;
using MongoDB;
using System.Threading.Tasks;


namespace EMPIMS.BLL.DBHelper
{
    /// <summary>
    /// mongodb的封装类。
    /// </summary>
    public class MongoDBHelper<T>
    {
        //获取web.config中的配置
        //public static readonly string connectionString_Default = MD5Encrypt.Decrypto(ConfigurationManager.ConnectionStrings["DBConnection"].ToString());
        //public static readonly string database_Default = MD5Encrypt.Decrypto(ConfigurationManager.ConnectionStrings["MongoDB"].ToString());

        #region MongoService
        //private string Database { get { return database_Default; } }
        //private string Connection { get { return connectionString_Default; } }

        private string collectName
        {
            get
            {
                return typeof(T).Name;
            }
        }

        //private MongoServer GetService(string conn)
        //{
        //    var client = new MongoClient(conn);
        //    return client.GetServer();
        //}

        //protected MongoServer GetServer()
        //{
        //    return GetService(Connection);
        //}

        //protected MongoDatabase GetDatabase()
        //{
        //    return GetServer().GetDatabase(Database);
        //}


        Singleton single = Singleton.Instance;
        public MongoCollection GetCollection()
        {
            // var service = GetServer();
            // var service = single.GetService();
            var database = single.GetDataBase();
            if (!database.CollectionExists(collectName))
                database.CreateCollection(collectName);
            return database.GetCollection(collectName);
        }
        #endregion


        #region 新增
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Insert(T t)
        {
            var result = GetCollection().Insert<T>(t);
            return result.Ok;
        }

        public bool Insert(BsonDocument doc)
        {
            var result = GetCollection().Insert(doc);
            return result.Ok;
        }

        public void BatchInsert(IList<T> list)
        {
            GetCollection().InsertBatch(list);
        }

        public bool Save(T t)
        {
            var result = GetCollection().Save<T>(t);
            return result.Ok;
        }

        #endregion
        #region 修改
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="query">条件</param>
        /// <param name="update"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public bool Update(IMongoQuery query, IMongoUpdate update, UpdateFlags flags = UpdateFlags.None)
        {
            var result = GetCollection().Update(query, update, flags);
            return result.Ok;
        }

        public bool Update(string id, T t)
        {
            var doc = new UpdateDocument(BsonExtensionMethods.ToBsonDocument(t));
            return GetCollection().Update(Query.EQ("_id", id), doc).Ok;
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="query">条件</param>
        /// <param name="t">实体</param>
        /// <param name="updateField">需要更新的字段</param>
        /// <returns></returns>
        public bool Update(IMongoQuery query, T t, string[] updateField)
        {
            UpdateBuilder update = new UpdateBuilder();
            var doc = new UpdateDocument(BsonExtensionMethods.ToBsonDocument(t));
            var name = "";
            string[] names = doc.Names.ToArray();
            for (int i = 0; i < names.Length; i++)
            {
                name = names[i];
                if (!updateField.Contains(name) && name != "")
                {
                    doc.Remove(name);
                }
                else
                {
                    update.Set(name, doc[name] == BsonNull.Value ? "" : doc[name]);
                }
            }

            return GetCollection().Update(query, update).Ok;
        }


        public bool FindAndModify(string id, Dictionary<string, object> Updates)
        {
            UpdateBuilder builder = null;
            foreach (var v in Updates)
            {
                BsonValue value = v.Value.ToString();
                builder.AddToSet(v.Key, value.AsBsonValue);//此处的转换错误一直没解决掉
            }
            return GetCollection().Update(Query.EQ("_id", id), builder).Ok;
        }

        #endregion
        #region 删除
        public bool Remove(IMongoQuery query)
        {
            var result = GetCollection().Remove(query);
            return result.Ok;
        }
        public void RemoveByIds(string[] ids)
        {
            var query = Query.In("_id", ToBsonArray(ids));
            GetCollection().Remove(query);
        }
        public void RemoveById(string id)
        {
            RemoveByIds(new string[1] { id });
        }
        #endregion
        #region 查询
        /// <summary>
        /// 判断数据是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsExists(string id)
        {
            return Count(Query.EQ("_id", id)) > 0;
        }

        /// <summary>
        /// 查询数量
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public long Count(IMongoQuery query)
        {
            return GetCollection().Count(query);
        }

        public async Task<long> CountAsync(IMongoQuery query)
        {
            return await Task.Run(() => GetCollection().Count(query));
        }

        public IList<BsonValue> Distict(string key, IMongoQuery query)
        {
            return GetCollection().Distinct(key, query).ToList();
        }



        public T FindOneById(string id, params string[] fields)
        {
            var query = Query.EQ("_id", new ObjectId(id));
            return FindOne(query, fields);
        }


        public T FindOne(IMongoQuery query, params string[] fields)
        {
            var result = Find(1, query, SortBy.Null, fields);
            if (result.Count > 0)
            {
                return result.FirstOrDefault();
            }
            return default(T);
        }




        public IList<T> FindAll(params string[] fields)
        {
            var cursor = GetCollection().FindAllAs<T>().SetFlags(QueryFlags.NoCursorTimeout);
            if (fields != null && fields.Length > 0)
            {
                cursor.SetFields(fields);
            }
            return cursor.ToList();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="listQuery">查询条件</param>
        /// <param name="pagination">分页参数</param>
        /// <returns></returns>
        public IList<T> Find(IMongoQuery[] listQuery, Pagination pagination)
        {
            IMongoQuery query = null;
            if (listQuery.Length > 0)
            {
                query = Query.And(listQuery.ToArray());
            }

            var collections = GetCollection().FindAs<T>(query).SetLimit(2000);

            if (pagination != null)
            {
                //pagination.records = Count(query).ToInt();
                IMongoSortBy sort = pagination.sord.ToLower() == "asc" ? SortBy.Ascending(pagination.sidx) : SortBy.Descending(pagination.sidx);
                collections = collections.SetSortOrder(sort).SetSkip((pagination.page - 1) * pagination.rows).SetLimit(pagination.rows);
            }

            return collections.ToList();
        }

        public IList<T> FindByIds(IEnumerable<string> ids, params string[] fields)
        {
            return Find(Query.In("_id", ToBsonArray(ids)));
        }



        public IList<T> Find(IMongoQuery query, params string[] fields)
        {
            return Find(query, SortBy.Null, fields);
        }



        public IList<T> Find(IMongoQuery query, IMongoSortBy sortBy, params string[] fields)
        {
            return Find(0, query, sortBy, fields);
        }



        public IList<T> Find(int topCount, IMongoQuery query, params string[] fields)
        {
            return Find(topCount, query, fields);
        }


        public IList<T> Find(int topCount, IMongoQuery query, IMongoSortBy sortBy, params string[] fields)
        {
            var cursor = GetCollection().FindAs<T>(query);

            BsonDocument document = new BsonDocument();

            if (topCount > 0)
            {
                cursor = cursor.SetLimit(topCount);
            }
            if (sortBy != null && sortBy != SortBy.Null)
            {
                cursor = cursor.SetSortOrder(sortBy);
            }
            if (fields != null && fields.Length > 0)
            {
                cursor = cursor.SetFields(fields);
            }
            return cursor.ToList();
        }


        #endregion
        #region 分页
        /// <summary>
        ///  数据量大时 性能不好
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sortBy"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="totalCount"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public IList<T> FindPage(IMongoQuery query, IMongoSortBy sortBy, int page, int size, out long totalCount, params string[] fields)
        {
            int skipCount = 0;
            if (page > 1)
                skipCount = (page - 1) * size;
            else
                page = 1;

            totalCount = Count(query);
            var list = new List<T>();

            if (totalCount > 0)
                list = GetCollection().FindAs<T>(query).SetFlags(QueryFlags.NoCursorTimeout).SetSortOrder(sortBy).SetSkip(skipCount).SetLimit(size).SetFields(fields).ToList();
            return list;
        }

        public IList<T> FindPage(string lastObjId, IMongoQuery query, int pageSize,
           out long totalCount, params string[] fields)
        {
            return FindPage(lastObjId, query, pageSize, out totalCount, true, fields);
        }
        public IList<T> FindPage(string lastObjId, IMongoQuery query, int pageSize, params string[] fields)
        {
            long totalCount;
            return FindPage(lastObjId, query, pageSize, out totalCount, false, fields);
        }

        /// <summary>
        /// 获取前一页的最后一条记录，查询之后的指定条记录 
        /// </summary>
        /// <param name="lastObjId">首次加载为string.Emtpy,当前页最后一条数据的Id</param>
        /// <param name="query"></param>
        /// <param name="size"></param>
        /// <param name="totalCount"></param>
        /// <param name="isOutTotalCount"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public IList<T> FindPage(string lastObjId, IMongoQuery query, int size, out long totalCount, bool isOutTotalCount, params string[] fields)
        {
            var pQuery = Query.Exists("_id");

            if (!string.IsNullOrEmpty(lastObjId))
            {
                pQuery = Query.And(pQuery, Query.LT("_id", lastObjId));
            }
            pQuery = Query.And(pQuery, query);
            var cursor = Find(size, query, SortBy.Descending("_id"), fields);
            if (isOutTotalCount)
            {
                totalCount = Count(query);
            }
            else
            {
                totalCount = 0;
            }
            return cursor.ToList();
        }
        #endregion
        #region 分组
        public IList<BsonDocument> Group(GroupArgs args)
        {
            return GetCollection().Group(args).ToList();
        }
        #endregion


        #region MapReduce
        public MapReduceResult MapReduce(MapReduceArgs args)
        {
            return GetCollection().MapReduce(args);
        }
        #endregion
        #region 封装的方法
        /// <summary>
        ///  CreateRegex
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="regOpt"></param>
        /// <returns></returns>
        public Regex CreateRegex(string reg, RegexOptions regOpt = RegexOptions.IgnoreCase)
        {
            return new Regex(reg, regOpt);
        }
        /// <summary>
        /// 转BsonArray类型
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public BsonArray ToBsonArray(System.Collections.IEnumerable list)
        {
            return new BsonArray(list);
        }
        public IList<BsonDocument> Group(IMongoQuery query, string key, BsonDocument initial, string reduce, string finalize = "")
        {
            return this.GetCollection().Group(query, new BsonJavaScript(key), initial, new BsonJavaScript(reduce), new BsonJavaScript(finalize)).ToList();
        }
        #endregion




        public IList<BsonDocument> MyFindTest()
        {
            string[] fields = { };
            var collecion = GetCollection();
            MongoCursor<BsonDocument> coursor = collecion.FindAllAs<BsonDocument>();
            if (fields != null && fields.Length > 0)
            {
                coursor.SetFields(fields);
            }
            return coursor.ToList<BsonDocument>();
        }
    }
}