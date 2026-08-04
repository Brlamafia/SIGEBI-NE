-- EJERCICIO 9.2
SET SERVEROUTPUT ON
DECLARE
  e_campo_obligatorio EXCEPTION;
  PRAGMA EXCEPTION_INIT(e_campo_obligatorio, -1400);
BEGIN
  INSERT INTO employees (employee_id, last_name)
  VALUES (NULL, 'Monasterio');
EXCEPTION
  WHEN e_campo_obligatorio THEN
    DBMS_OUTPUT.PUT_LINE('No se puede registrar la informacion...');
    DBMS_OUTPUT.PUT_LINE(SQLERRM);
END;
/
