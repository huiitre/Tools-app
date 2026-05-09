package fr.huiitre.tools.modules.core.common.infrastructure;

import java.sql.Connection;
import java.sql.SQLException;

import javax.sql.DataSource;

import org.springframework.jdbc.datasource.DataSourceUtils;

public abstract class AbstractPostgresRepository {

    protected final DataSource dataSource;

    protected AbstractPostgresRepository(DataSource dataSource) {
        this.dataSource = dataSource;
    }

    protected Connection openConnection() throws SQLException {
        return DataSourceUtils.getConnection(dataSource);
    }

    protected RuntimeException sqlError(String message, SQLException e) {
        return new RuntimeException(message, e);
    }
}
