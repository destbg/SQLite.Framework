using System.Reflection;
using SQLitePCL;

namespace SQLite.Framework.Tests.NoOp;

public class NoOpSQLite : ISQLite3Provider
{
    public static bool BackupInitReturnsNull;
    public static int BackupStepReturnCode = 101;
    public static int ErrCode;
    public static int BeginStepReturnCode = 101;

    public string GetNativeLibraryName()
    {
        return "NoOpSQLite";
    }

    public int sqlite3_backup_finish(nint backup)
    {
        return backup == 0 ? 0 : 1;
    }

    public sqlite3_backup sqlite3_backup_init(sqlite3 destDb, utf8z destName, sqlite3 sourceDb, utf8z sourceName)
    {
        return BackupInitReturnsNull ? null! : sqlite3_backup.From(0);
    }

    public int sqlite3_backup_pagecount(sqlite3_backup backup)
    {
        return 0;
    }

    public int sqlite3_backup_remaining(sqlite3_backup backup)
    {
        return 0;
    }

    public int sqlite3_backup_step(sqlite3_backup backup, int nPage)
    {
        return BackupStepReturnCode;
    }

    public int sqlite3_bind_blob(sqlite3_stmt stmt, int index, ReadOnlySpan<byte> blob)
    {
        return 0;
    }

    public int sqlite3_bind_double(sqlite3_stmt stmt, int index, double val)
    {
        return 0;
    }

    public int sqlite3_bind_int(sqlite3_stmt stmt, int index, int val)
    {
        return 0;
    }

    public int sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val)
    {
        return 0;
    }

    public int sqlite3_bind_null(sqlite3_stmt stmt, int index)
    {
        return 0;
    }

    public int sqlite3_bind_parameter_count(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_bind_parameter_index(sqlite3_stmt stmt, utf8z strName)
    {
        return 1;
    }

    public utf8z sqlite3_bind_parameter_name(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public int sqlite3_bind_text(sqlite3_stmt stmt, int index, ReadOnlySpan<byte> text)
    {
        return 0;
    }

    public int sqlite3_bind_text(sqlite3_stmt stmt, int index, utf8z text)
    {
        return 0;
    }

    public int sqlite3_bind_text16(sqlite3_stmt stmt, int index, ReadOnlySpan<char> text)
    {
        return 0;
    }

    public int sqlite3_bind_zeroblob(sqlite3_stmt stmt, int index, int size)
    {
        return 0;
    }

    public int sqlite3_blob_bytes(sqlite3_blob blob)
    {
        return 0;
    }

    public int sqlite3_blob_close(nint blob)
    {
        return 0;
    }

    public int sqlite3_blob_open(sqlite3 db, utf8z db_utf8, utf8z table_utf8, utf8z col_utf8, long rowid, int flags, out sqlite3_blob blob)
    {
        MethodInfo fromMethod = typeof(sqlite3_blob).GetMethod("From", BindingFlags.Public | BindingFlags.Static)!;
        blob = (sqlite3_blob)fromMethod.Invoke(null, [0])!;
        return 0;
    }

    public int sqlite3_blob_read(sqlite3_blob blob, Span<byte> b, int offset)
    {
        return 0;
    }

    public int sqlite3_blob_reopen(sqlite3_blob blob, long rowid)
    {
        return 0;
    }

    public int sqlite3_blob_write(sqlite3_blob blob, ReadOnlySpan<byte> b, int offset)
    {
        return 0;
    }

    public int sqlite3_busy_timeout(sqlite3 db, int ms)
    {
        return 0;
    }

    public int sqlite3_changes(sqlite3 db)
    {
        return 0;
    }

    public int sqlite3_clear_bindings(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_close(nint db)
    {
        return 0;
    }

    public int sqlite3_close_v2(nint db)
    {
        return 0;
    }

    public ReadOnlySpan<byte> sqlite3_column_blob(sqlite3_stmt stmt, int index)
    {
        return new ReadOnlySpan<byte>();
    }

    public int sqlite3_column_bytes(sqlite3_stmt stmt, int index)
    {
        return 0;
    }

    public int sqlite3_column_count(sqlite3_stmt stmt)
    {
        return 0;
    }

    public utf8z sqlite3_column_database_name(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public utf8z sqlite3_column_decltype(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public double sqlite3_column_double(sqlite3_stmt stmt, int index)
    {
        return 0;
    }

    public int sqlite3_column_int(sqlite3_stmt stmt, int index)
    {
        return 0;
    }

    public long sqlite3_column_int64(sqlite3_stmt stmt, int index)
    {
        return 0;
    }

    public utf8z sqlite3_column_name(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public utf8z sqlite3_column_origin_name(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public utf8z sqlite3_column_table_name(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public utf8z sqlite3_column_text(sqlite3_stmt stmt, int index)
    {
        return new utf8z();
    }

    public int sqlite3_column_type(sqlite3_stmt stmt, int index)
    {
        return 1;
    }

    public void sqlite3_commit_hook(sqlite3 db, delegate_commit func, object v)
    {
    }
    public utf8z sqlite3_compileoption_get(int n)
    {
        return new utf8z();
    }

    public int sqlite3_compileoption_used(utf8z sql)
    {
        return 0;
    }

    public int sqlite3_complete(utf8z sql)
    {
        return 0;
    }

    public int sqlite3_config(int op)
    {
        return 0;
    }

    public int sqlite3_config(int op, int val)
    {
        return 0;
    }

    public int sqlite3_config_log(delegate_log func, object v)
    {
        return 0;
    }

    public int sqlite3_create_collation(sqlite3 db, byte[] name, object v, delegate_collation func)
    {
        return 0;
    }

    public int sqlite3_create_function(sqlite3 db, byte[] name, int nArg, int flags, object v, delegate_function_scalar func)
    {
        return 0;
    }

    public int sqlite3_create_function(sqlite3 db, byte[] name, int nArg, int flags, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final)
    {
        return 0;
    }

    public int sqlite3_data_count(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_db_config(sqlite3 db, int op, utf8z val)
    {
        return 0;
    }

    public int sqlite3_db_config(sqlite3 db, int op, int val, out int result)
    {
        result = 0;
        return 0;
    }

    public int sqlite3_db_config(sqlite3 db, int op, nint ptr, int int0, int int1)
    {
        return 0;
    }

    public utf8z sqlite3_db_filename(sqlite3 db, utf8z att)
    {
        return new utf8z();
    }

    public nint sqlite3_db_handle(nint stmt)
    {
        return 0;
    }

    public int sqlite3_db_readonly(sqlite3 db, utf8z dbName)
    {
        return 0;
    }

    public int sqlite3_db_status(sqlite3 db, int op, out int current, out int highest, int resetFlg)
    {
        highest = 0;
        current = 0;
        return 0;
    }

    public int sqlite3_deserialize(sqlite3 db, utf8z schema, nint data, long szDb, long szBuf, int flags)
    {
        return 0;
    }

    public int sqlite3_enable_load_extension(sqlite3 db, int enable)
    {
        return 0;
    }

    public int sqlite3_enable_shared_cache(int enable)
    {
        return 0;
    }

    public int sqlite3_errcode(sqlite3 db)
    {
        return ErrCode;
    }

    public utf8z sqlite3_errmsg(sqlite3 db)
    {
        return new utf8z();
    }

    public utf8z sqlite3_errstr(int rc)
    {
        return new utf8z();
    }

    public int sqlite3_exec(sqlite3 db, utf8z sql, delegate_exec callback, object user_data, out nint errMsg)
    {
        errMsg = 0;
        return 0;
    }

    public int sqlite3_extended_errcode(sqlite3 db)
    {
        return 0;
    }

    public int sqlite3_extended_result_codes(sqlite3 db, int onoff)
    {
        return 0;
    }

    public int sqlite3_finalize(nint stmt)
    {
        stmtIsSelect.TryRemove(stmt, out _);
        stmtRowsRemaining.TryRemove(stmt, out _);
        return 0;
    }

    public void sqlite3_free(nint p)
    {
    }

    public int sqlite3_get_autocommit(sqlite3 db)
    {
        return 0;
    }

    public long sqlite3_hard_heap_limit64(long n)
    {
        return 0;
    }

    public int sqlite3_initialize()
    {
        return 0;
    }

    public void sqlite3_interrupt(sqlite3 db)
    {
    }

    public int sqlite3_key(sqlite3 db, ReadOnlySpan<byte> key)
    {
        return 0;
    }

    public int sqlite3_keyword_count()
    {
        return 0;
    }

    public int sqlite3_keyword_name(int i, out string name)
    {
        name = string.Empty;
        return 0;
    }

    public int sqlite3_key_v2(sqlite3 db, utf8z dbname, ReadOnlySpan<byte> key)
    {
        return 0;
    }

    public long sqlite3_last_insert_rowid(sqlite3 db)
    {
        return 0;
    }

    public utf8z sqlite3_libversion()
    {
        return new utf8z();
    }

    public int sqlite3_libversion_number()
    {
        return 0;
    }

    public int sqlite3_limit(sqlite3 db, int id, int newVal)
    {
        return 0;
    }

    public int sqlite3_load_extension(sqlite3 db, utf8z zFile, utf8z zProc, out utf8z pzErrMsg)
    {
        pzErrMsg = new utf8z();
        return 0;
    }

    public void sqlite3_log(int errcode, utf8z s)
    {
    }

    public nint sqlite3_malloc(int n)
    {
        return 0;
    }

    public nint sqlite3_malloc64(long n)
    {
        return 0;
    }

    public long sqlite3_memory_highwater(int resetFlag)
    {
        return 0;
    }

    public long sqlite3_memory_used()
    {
        return 0;
    }

    public nint sqlite3_next_stmt(sqlite3 db, nint stmt)
    {
        return 0;
    }

    public int sqlite3_open(utf8z filename, out nint db)
    {
        db = 0;
        return 0;
    }

    public int sqlite3_open_v2(utf8z filename, out nint db, int flags, utf8z vfs)
    {
        db = 0;
        return 0;
    }

    public int sqlite3_prepare_v2(sqlite3 db, ReadOnlySpan<byte> sql, out nint stmt, out ReadOnlySpan<byte> remain)
    {
        stmt = AllocStmt(System.Text.Encoding.UTF8.GetString(sql));
        remain = new ReadOnlySpan<byte>();
        return 0;
    }

    public int sqlite3_prepare_v2(sqlite3 db, utf8z sql, out nint stmt, out utf8z remain)
    {
        stmt = AllocStmt(sql.utf8_to_string());
        remain = new utf8z();
        return 0;
    }

    public int sqlite3_prepare_v3(sqlite3 db, ReadOnlySpan<byte> sql, uint flags, out nint stmt, out ReadOnlySpan<byte> remain)
    {
        stmt = AllocStmt(System.Text.Encoding.UTF8.GetString(sql));
        remain = new ReadOnlySpan<byte>();
        return 0;
    }

    public int sqlite3_prepare_v3(sqlite3 db, utf8z sql, uint flags, out nint stmt, out utf8z remain)
    {
        stmt = AllocStmt(sql.utf8_to_string());
        remain = new utf8z();
        return 0;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<nint, bool> stmtIsBegin = new();

    private static nint AllocStmt(string sql)
    {
        nint id = (nint)System.Threading.Interlocked.Increment(ref nextStmtId);
        string trimmed = sql.TrimStart();
        stmtIsSelect[id] = trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
        stmtIsBegin[id] = trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase);
        return id;
    }

    public void sqlite3_profile(sqlite3 db, delegate_profile func, object v)
    {
    }

    public void sqlite3_progress_handler(sqlite3 db, int instructions, delegate_progress func, object v)
    {
    }

    public int sqlite3_rekey(sqlite3 db, ReadOnlySpan<byte> key)
    {
        return 0;
    }

    public int sqlite3_rekey_v2(sqlite3 db, utf8z dbname, ReadOnlySpan<byte> key)
    {
        return 0;
    }

    public int sqlite3_reset(sqlite3_stmt stmt)
    {
        stmtRowsRemaining.TryRemove(stmt.DangerousGetHandle(), out _);
        return 0;
    }

    public void sqlite3_result_blob(nint context, ReadOnlySpan<byte> val)
    {
    }

    public void sqlite3_result_double(nint context, double val)
    {
    }

    public void sqlite3_result_error(nint context, ReadOnlySpan<byte> strErr)
    {
    }

    public void sqlite3_result_error(nint context, utf8z strErr)
    {
    }

    public void sqlite3_result_error_code(nint context, int code)
    {
    }

    public void sqlite3_result_error_nomem(nint context)
    {
    }

    public void sqlite3_result_error_toobig(nint context)
    {
    }

    public void sqlite3_result_int(nint context, int val)
    {
    }

    public void sqlite3_result_int64(nint context, long val)
    {
    }

    public void sqlite3_result_null(nint context)
    {
    }

    public void sqlite3_result_text(nint context, ReadOnlySpan<byte> val)
    {
    }

    public void sqlite3_result_text(nint context, utf8z val)
    {
    }

    public void sqlite3_result_zeroblob(nint context, int n)
    {
    }

    public void sqlite3_rollback_hook(sqlite3 db, delegate_rollback func, object v)
    {
    }

    public nint sqlite3_serialize(sqlite3 db, utf8z schema, out long size, int flags)
    {
        size = 0;
        return 0;
    }

    public int sqlite3_set_authorizer(sqlite3 db, delegate_authorizer authorizer, object user_data)
    {
        return 0;
    }

    public int sqlite3_shutdown()
    {
        return 0;
    }

    public int sqlite3_snapshot_cmp(sqlite3_snapshot p1, sqlite3_snapshot p2)
    {
        return 0;
    }

    public void sqlite3_snapshot_free(nint snap)
    {
    }

    public int sqlite3_snapshot_get(sqlite3 db, utf8z schema, out nint snap)
    {
        snap = 0;
        return 0;
    }

    public int sqlite3_snapshot_open(sqlite3 db, utf8z schema, sqlite3_snapshot snap)
    {
        return 0;
    }

    public int sqlite3_snapshot_recover(sqlite3 db, utf8z name)
    {
        return 0;
    }

    public long sqlite3_soft_heap_limit64(long n)
    {
        return 0;
    }

    public utf8z sqlite3_sourceid()
    {
        return new utf8z();
    }

    public utf8z sqlite3_sql(sqlite3_stmt stmt)
    {
        return new utf8z();
    }

    public int sqlite3_status(int op, out int current, out int highwater, int resetFlag)
    {
        current = 0;
        highwater = 0;
        return 0;
    }

    public static int RowsPerQuery;

    private static long nextStmtId;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<nint, bool> stmtIsSelect = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<nint, int> stmtRowsRemaining = new();

    public int sqlite3_step(sqlite3_stmt stmt)
    {
        nint id = stmt.DangerousGetHandle();
        if (stmtIsBegin.TryGetValue(id, out bool isBegin) && isBegin)
        {
            return BeginStepReturnCode;
        }

        if (stmtIsSelect.TryGetValue(id, out bool isSelect) && isSelect)
        {
            int remaining = stmtRowsRemaining.GetOrAdd(id, _ => RowsPerQuery);
            if (remaining > 0)
            {
                stmtRowsRemaining[id] = remaining - 1;
                return 100;
            }
        }

        return 101;
    }

    public int sqlite3_stmt_busy(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_stmt_isexplain(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_stmt_readonly(sqlite3_stmt stmt)
    {
        return 0;
    }

    public int sqlite3_stmt_status(sqlite3_stmt stmt, int op, int resetFlg)
    {
        return 0;
    }

    public int sqlite3_stricmp(nint p, nint q)
    {
        return 0;
    }

    public int sqlite3_strnicmp(nint p, nint q, int n)
    {
        return 0;
    }

    public int sqlite3_table_column_metadata(sqlite3 db, utf8z dbName, utf8z tblName, utf8z colName, out utf8z dataType, out utf8z collSeq, out int notNull, out int primaryKey, out int autoInc)
    {
        dataType = new utf8z();
        collSeq = new utf8z();
        notNull = 0;
        primaryKey = 0;
        autoInc = 0;
        return 0;
    }

    public int sqlite3_threadsafe()
    {
        return 0;
    }

    public int sqlite3_total_changes(sqlite3 db)
    {
        return 0;
    }

    public void sqlite3_trace(sqlite3 db, delegate_trace func, object v)
    {
    }

    public void sqlite3_update_hook(sqlite3 db, delegate_update func, object v)
    {
    }

    public ReadOnlySpan<byte> sqlite3_value_blob(nint p)
    {
        return new ReadOnlySpan<byte>();
    }

    public int sqlite3_value_bytes(nint p)
    {
        return 0;
    }

    public double sqlite3_value_double(nint p)
    {
        return 0;
    }

    public int sqlite3_value_int(nint p)
    {
        return 0;
    }

    public long sqlite3_value_int64(nint p)
    {
        return 0;
    }

    public utf8z sqlite3_value_text(nint p)
    {
        return new utf8z();
    }

    public int sqlite3_value_type(nint p)
    {
        return 0;
    }

    public int sqlite3_wal_autocheckpoint(sqlite3 db, int n)
    {
        return 0;
    }

    public int sqlite3_wal_checkpoint(sqlite3 db, utf8z dbName)
    {
        return 0;
    }

    public int sqlite3_wal_checkpoint_v2(sqlite3 db, utf8z dbName, int eMode, out int logSize, out int framesCheckPointed)
    {
        logSize = 0;
        framesCheckPointed = 0;
        return 0;
    }

    public int sqlite3_win32_set_directory(int typ, utf8z path)
    {
        return 0;
    }

    public int sqlite3__vfs__delete(utf8z vfs, utf8z pathname, int syncDir)
    {
        return 0;
    }

}