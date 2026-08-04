-- EJERCICIO 9.3
SET SERVEROUTPUT ON
BEGIN
  EXECUTE IMMEDIATE 'DROP TABLE ct_errors PURGE';
EXCEPTION
  WHEN OTHERS THEN
    IF SQLCODE <> -942 THEN RAISE; END IF;
END;
/

CREATE TABLE ct_errors (
  e_user VARCHAR2(10),
  e_date DATE,
  e_code NUMBER(6),
  e_msg  VARCHAR2(255)
);

DECLARE
  v_code NUMBER;
  v_msg  VARCHAR2(255);
BEGIN
  INSERT INTO departments (department_id, department_name)
  VALUES (NULL, 'Software Architect');
EXCEPTION
  WHEN OTHERS THEN
    v_code := SQLCODE;
    v_msg  := SQLERRM;
    INSERT INTO ct_errors VALUES (USER, SYSDATE, v_code, v_msg);
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('No se pudo registrar la informacion, mensaje de error grabado en la tabla CT_ERRORS');
END;
/

COLUMN e_user FORMAT A10
COLUMN e_date FORMAT A19
COLUMN e_msg FORMAT A65
SELECT e_user, TO_CHAR(e_date, 'DD/MM/YYYY HH24:MI:SS') e_date,
       e_code, e_msg
  FROM ct_errors;
